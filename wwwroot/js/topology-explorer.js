(function (root, factory) {
  if (typeof module === 'object' && module.exports) module.exports = factory();
  else root.TopologyExplorer = factory();
})(typeof globalThis !== 'undefined' ? globalThis : this, function () {
  'use strict';
  function create(elements) {
    const nodes = elements.filter(element => !('source' in element.data));
    const edges = elements.filter(element => 'source' in element.data);
    const byId = new Map(nodes.map(node => [node.data.id, node]));
    const outgoing = new Map(nodes.map(node => [node.data.id, []]));
    const incoming = new Set();
    for (const edge of edges) { outgoing.get(edge.data.source).push(edge); incoming.add(edge.data.target); }
    const seeds = [];
    const reachable = new Set();
    function seed(node) {
      if (reachable.has(node.data.id)) return;
      seeds.push(node);
      const queue = [node.data.id]; reachable.add(node.data.id);
      for (let index = 0; index < queue.length; index++) {
        for (const edge of outgoing.get(queue[index])) {
          if (!reachable.has(edge.data.target)) { reachable.add(edge.data.target); queue.push(edge.data.target); }
        }
      }
    }
    nodes.filter(node => node.classes?.split(' ').includes('requested')).forEach(seed);
    nodes.filter(node => !incoming.has(node.data.id)).forEach(seed);
    nodes.forEach(seed); // Disconnected cycles remain discoverable.
    const visibleNodes = new Set(seeds.map(node => node.data.id));
    const visibleEdges = new Set();
    return {
      initial: seeds,
      expand(id) {
        if (!visibleNodes.has(id)) return [];
        const addedNodes = [], addedEdges = [];
        for (const edge of outgoing.get(id)) {
          if (!visibleNodes.has(edge.data.target)) { visibleNodes.add(edge.data.target); addedNodes.push(byId.get(edge.data.target)); }
          if (!visibleEdges.has(edge.data.id)) { visibleEdges.add(edge.data.id); addedEdges.push(edge); }
        }
        return [...addedNodes, ...addedEdges];
      }
    };
  }
  return { create };
});
