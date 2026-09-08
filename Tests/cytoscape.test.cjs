const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
// Load the vendored UMD scripts independently of the parent React package's ESM setting.
function loadUmd(relativePath) {
  const module = { exports: {} };
  new Function('module', 'exports', fs.readFileSync(path.join(__dirname, relativePath), 'utf8'))(module, module.exports);
  return module.exports;
}
const cytoscape = loadUmd('../wwwroot/lib/cytoscape/cytoscape.min.js');
const graph = loadUmd('../wwwroot/js/topology-graph.js');
const response = JSON.parse(fs.readFileSync(path.join(__dirname, '../Data/service-topology.json'), 'utf8'));
function create(data) {
  return cytoscape({ headless: true, styleEnabled: true, elements: graph.elements(data, data.nodes[0]?.id),
    style: graph.style, layout: { ...graph.layout(), fit: false } });
}
const cy = create(response);
assert.equal(cy.nodes().length, 100);
assert.equal(cy.edges().length, 135);
assert.equal(cy.nodes('.requested').length, 1);
cy.edges().forEach((edge, index) => {
  assert.equal(edge.source().data('serviceId'), response.edges[index].source);
  assert.equal(edge.target().data('serviceId'), response.edges[index].target);
  assert.equal(edge.data('callType'), response.edges[index].callType);
  assert.equal(edge.style('label'), response.edges[index].callType);
  assert.equal(edge.style('target-arrow-shape'), 'triangle');
});
const nodes = cy.nodes();
for (const node of nodes) assert.ok(Number.isFinite(node.position('x')) && Number.isFinite(node.position('y')));
for (let i = 0; i < nodes.length; i++) for (let j = i + 1; j < nodes.length; j++) {
  assert.ok(Math.abs(nodes[i].position('x') - nodes[j].position('x')) >= 235 || Math.abs(nodes[i].position('y') - nodes[j].position('y')) >= 60, 'Overlapping cards');
}
cy.destroy();
const tricky = create({ nodes: [{id:'e:0',label:'<script>test</script>'},{id:'a',label:'A'},{id:'orphan',label:'Orphan'}],
  edges: [{source:'e:0',target:'a',callType:'REST'},{source:'e:0',target:'a',callType:'SOAP'},{source:'a',target:'e:0',callType:'gRPC'},{source:'a',target:'a',callType:'NORMAL'}], dependencies:[] });
assert.equal(tricky.nodes().length, 3);
assert.equal(tricky.edges().length, 4);
tricky.nodes().forEach(node => assert.ok(Number.isFinite(node.position('x')) && Number.isFinite(node.position('y'))));
assert.equal(tricky.getElementById('n:0').data('label'), '<script>test</script>');
tricky.destroy();
const empty = create({nodes:[],edges:[],dependencies:[]});
assert.equal(empty.elements().length,0); empty.destroy();
console.log('PASS: real Cytoscape layout, 100 nodes/135 edges, callType labels/arrows, non-overlapping positions, cycles, parallel calls, self-calls, isolated and empty graphs.');
