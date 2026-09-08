/* Shared graph mapping used by the browser and headless Cytoscape tests. */
(function (root, factory) {
  if (typeof module === 'object' && module.exports) module.exports = factory();
  else root.TopologyGraph = factory();
})(typeof globalThis !== 'undefined' ? globalThis : this, function () {
  'use strict';
  const knownColors = { REST: '#336ac0', SOAP: '#8052b8', gRPC: '#188575', 'LOCAL REST': '#ad710f', NORMAL: '#677587' };
  function color(type) { return knownColors[type] || '#677587'; }
  function elements(response, requestedService) {
    const ids = new Map(response.nodes.map((node, index) => [node.id, `n:${index}`]));
    const targets = new Set(response.edges.map(edge => edge.target));
    const sources = new Set(response.edges.map(edge => edge.source));
    return [
      ...response.nodes.map(node => ({
        data: { id: ids.get(node.id), serviceId: node.id, label: node.label },
        classes: [node.id === requestedService || node.label === requestedService ? 'requested' : '',
          !targets.has(node.id) ? 'entry' : '', !sources.has(node.id) ? 'leaf' : ''].join(' ')
      })),
      // Index-based edge IDs preserve parallel and identical calls without colliding with node IDs.
      ...response.edges.map((edge, index) => ({ data: {
        id: `e:${index}`, source: ids.get(edge.source), target: ids.get(edge.target),
        callType: edge.callType, lineColor: color(edge.callType)
      } }))
    ];
  }
  const style = [
    { selector: 'node', style: { shape: 'round-rectangle', width: 235, height: 60,
      'background-color': '#ffffff', 'border-color': '#abb8ca', 'border-width': 1.5,
      label: 'data(label)', color: '#252830', 'font-size': 12, 'font-family': 'Arial',
      'text-valign': 'center', 'text-halign': 'center', 'text-wrap': 'ellipsis', 'text-max-width': 215 } },
    { selector: 'node.entry', style: { 'border-width': 3, 'border-color': '#e52336' } },
    { selector: 'node.leaf', style: { 'border-style': 'dashed' } },
    { selector: 'node.requested', style: { 'background-color': '#e52336', 'border-color': '#e52336', color: '#ffffff' } },
    { selector: 'edge', style: { width: 1.8, 'curve-style': 'bezier', 'target-arrow-shape': 'triangle',
      'target-arrow-color': 'data(lineColor)', 'line-color': 'data(lineColor)',
      label: 'data(callType)', color: 'data(lineColor)', 'font-size': 11,
      'text-rotation': 'autorotate', 'text-background-color': '#ffffff',
      'text-background-opacity': 0.95, 'text-background-padding': 4, 'text-border-opacity': 0,
      'arrow-scale': 1.1, 'control-point-step-size': 55 } },
    { selector: '.muted', style: { opacity: 0.14 } },
    { selector: 'node:selected', style: { 'border-width': 4, 'border-color': '#e52336' } },
    { selector: 'edge:selected', style: { width: 4, 'text-background-color': '#fff0f2' } }
  ];
  function layout() {
    return { name: 'breadthfirst', directed: true, circle: false, grid: false,
      spacingFactor: 1.35, avoidOverlap: true, nodeDimensionsIncludeLabels: true,
      animate: false, fit: true, padding: 40 };
  }
  return { elements, style, layout, color };
});
