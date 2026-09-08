const fs = require('node:fs'), path = require('node:path'), assert = require('node:assert/strict');
function load(file) { const module={exports:{}}; new Function('module','exports',fs.readFileSync(path.join(__dirname,file),'utf8'))(module,module.exports); return module.exports; }
const graph=load('../wwwroot/js/topology-graph.js');
const explorer=load('../wwwroot/js/topology-explorer.js');
const cytoscape=load('../wwwroot/lib/cytoscape/cytoscape.min.js');
const data=JSON.parse(fs.readFileSync(path.join(__dirname,'../Data/service-topology.json'),'utf8'));
const elements=graph.elements(data,data.nodes[0].id);
const state=explorer.create(elements);
assert.equal(state.initial.length,1);
assert.equal(state.initial[0].data.serviceId,data.nodes[0].id);
const cy=cytoscape({headless:true,elements:state.initial});
assert.equal(cy.edges().length,0);
const direct=data.edges.filter(edge=>edge.source===data.nodes[0].id);
cy.add(state.expand('n:0'));
assert.equal(cy.edges().length,direct.length);
assert.equal(cy.nodes().length,new Set([data.nodes[0].id,...direct.map(edge=>edge.target)]).size);
assert.equal(state.expand('n:0').length,0);
for(let pass=0;pass<100;pass++) for(const node of cy.nodes()) cy.add(state.expand(node.id()));
assert.equal(cy.nodes().length,100); assert.equal(cy.edges().length,135);
cy.destroy();
const special=graph.elements({nodes:[{id:'a',label:'A'},{id:'b',label:'B'},{id:'c',label:'C'}],edges:[{source:'a',target:'b',callType:'REST'},{source:'a',target:'b',callType:'SOAP'},{source:'b',target:'a',callType:'NORMAL'},{source:'b',target:'b',callType:'gRPC'}]},'a');
const branches=explorer.create(special);
assert.equal(branches.initial.length,2); // requested service plus isolated component
assert.equal(branches.expand('n:1').length,0); // hidden nodes cannot expand
assert.equal(branches.expand('n:0').length,3);
assert.equal(branches.expand('n:1').length,2);
assert.equal(branches.expand('n:1').length,0);
console.log('PASS: only initial roots, one-hop expansion, call types, no premature edges, idempotence, full reachability, cycles, parallel calls and isolated nodes.');
const allState=explorer.create(elements);
const allGraph=cytoscape({headless:true,elements:allState.initial});
allGraph.add(allState.expand('n:0'));
allGraph.add(allState.expandAll());
assert.equal(allGraph.nodes().length,100); assert.equal(allGraph.edges().length,135);
assert.equal(allState.expandAll().length,0); assert.equal(allState.expand('n:0').length,0);
allGraph.destroy();
console.log('PASS: expand all after partial expansion includes every node and edge without duplicates.');
