// Run only against the MVC-New application configured with Email:Mode=Pickup.
const assert=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path');
const base='http://localhost:5090';
const dataDir=path.join(__dirname,'../App_Data');
(async()=>{
  const initial=await fetch(base); assert.equal(initial.status,200);
  const html=await initial.text();
  assert.ok(html.includes('name="Request.Email"'));
  assert.ok(html.includes('mailto:support@abc.com'));
  assert.ok(!html.includes('class="eyebrow"'));
  const token=html.match(/<input[^>]*name="__RequestVerificationToken"[^>]*>/)[0].match(/value="([^"]+)"/)[1];
  const cookie=initial.headers.getSetCookie().map(x=>x.split(';')[0]).join('; ');
  async function post(email,days=1,csrf=true,force=true){const body=new URLSearchParams({'Request.ServiceName':'ACQUIRER_PROCESS_REST_001','Request.Days':String(days),'Request.Email':email});body.set('force',String(force));if(csrf)body.set('__RequestVerificationToken',token);return fetch(base+'/Analysis/Analyze',{method:'POST',headers:{Cookie:cookie},body,redirect:'manual'});}
  for(const email of ['', 'invalid', 'a\nb@example.com']) assert.equal((await post(email)).status,400);
  assert.equal((await post('test@example.invalid',2)).status,400);
  assert.equal((await post('test@example.invalid',1,false)).status,400);
  assert.equal((await fetch(base+'/Analysis/Analyze/not-a-guid')).status,404);
  assert.equal((await fetch(base+'/Analysis/Analyze/00000000-0000-0000-0000-000000000000')).status,404);
  const response=await post('test@example.invalid'); assert.equal(response.status,302);
  const location=response.headers.get('location'); assert.ok(location.includes('/Analysis/Started/'));
  const id=location.split('/').at(-1);
  const started=await fetch(base+location); assert.equal(started.status,200);
  assert.ok((await started.text()).includes('test@example.invalid'));
  let result;
  for(let i=0;i<20;i++){await new Promise(resolve=>setTimeout(resolve,500));result=await (await fetch(base+'/Analysis/Analyze/'+id)).text();if(result.includes('id="topology-data"'))break;}
  assert.ok(result.includes('id="topology-data"'));
  const topology=JSON.parse(result.match(/id="topology-data">([\s\S]*?)<\/script>/)[1]);
  assert.equal(topology.nodes.length,100);assert.equal(topology.edges.length,135);
  assert.equal((await fetch(base+'/Analysis/Analyze?id='+id)).status,200);
  assert.ok(result.includes('id="expand-all"'));
  const prior=await post('test@example.invalid',1,true,false); assert.equal(prior.status,200);
  const priorHtml=await prior.text(); assert.ok(priorHtml.includes('id="history-heading"')); assert.ok(priorHtml.includes('/Analysis/Analyze/'+id));
  const storedPath=path.join(dataDir,'Jobs',id.replaceAll('-','')+'.json');
  let job;
  for(let i=0;i<20;i++){job=JSON.parse(fs.readFileSync(storedPath,'utf8'));if(job.EmailState==='Prepared')break;await new Promise(resolve=>setTimeout(resolve,300));}
  assert.equal(job.EmailState,'Prepared');assert.equal(job.State,'Completed');
  assert.ok(fs.readdirSync(path.join(dataDir,'Mail')).some(name=>name.endsWith('.eml')));
  assert.equal((await fetch(base+'/App_Data/Jobs/'+id.replaceAll('-','')+'.json')).status,404);
  console.log('PASS: email validation, accepted-job redirect, ID/query routes, background completion, persisted results, pickup email, anti-forgery and private job files. ID='+id);
})();

