'use strict';

(() => {
  const form = document.querySelector('#analysis-form');
  form?.addEventListener('submit', () => {
    if (!form.checkValidity()) return;
    const button = form.querySelector('[type="submit"]');
    button.disabled = true;
    button.querySelector('span').textContent = 'Analiz ediliyor';
  });
  window.addEventListener('pageshow', () => {
    const button = form?.querySelector('[type="submit"]');
    if (button) { button.disabled = false; button.querySelector('span').textContent = 'Analiz Et'; }
  });
  const container = document.querySelector('#cy-topology');
  if (!container) return;
  const error = document.querySelector('#graph-error');
  const controls = [...document.querySelectorAll('[data-zoom]')];
  let cy;
  let explorer;
  try {
    if (typeof cytoscape !== 'function') throw new Error('Cytoscape is unavailable.');
    const response = JSON.parse(document.querySelector('#topology-data').textContent);
    explorer = TopologyExplorer.create(TopologyGraph.elements(response, container.dataset.serviceName));
    cy = cytoscape({ container, elements: explorer.initial,
      style: TopologyGraph.style, layout: TopologyGraph.layout(), minZoom: 0.005, maxZoom: 3,
      selectionType: 'single', boxSelectionEnabled: false });
  } catch (problem) {
    console.error('Topology initialization failed', problem);
    error.textContent = 'Topoloji yüklenemedi. Bağlantıları aşağıdaki metin görünümünden inceleyebilirsiniz.';
    error.hidden = false;
    controls.forEach(button => { button.disabled = true; });
    return;
  }
  document.querySelectorAll('[data-call-type]').forEach(item => {
    item.querySelector('i').style.backgroundColor = TopologyGraph.color(item.dataset.callType);
  });
  const detail = document.querySelector('.graph-selection');
  function clearSelection() {
    cy.elements().unselect().removeClass('muted');
    detail.hidden = true;
  }
  function describe(element) {
    cy.elements().removeClass('muted');
    const related = element.isNode() ? element.closedNeighborhood() : element.union(element.connectedNodes());
    cy.elements().difference(related).addClass('muted');
    detail.hidden = false;
    if (element.isNode()) {
      detail.querySelector('strong').textContent = element.data('label');
      detail.querySelector('p').textContent = `${element.data('serviceId')} · ${element.incomers('edge').length} gelen bağlantı · ${element.outgoers('edge').length} giden bağlantı`;
    } else {
      detail.querySelector('strong').textContent = `${element.source().data('label')} → ${element.target().data('label')}`;
      detail.querySelector('p').textContent = `Çağrı tipi: ${element.data('callType')}`;
    }
  }
  cy.on('tap', 'node, edge', event => {
    if (event.target.isNode()) {
      const additions = explorer.expand(event.target.id());
      if (additions.length) { cy.add(additions); cy.layout(TopologyGraph.layout()).run(); }
    }
    clearSelection(); event.target.select(); describe(event.target);
  });
  cy.on('tap', event => { if (event.target === cy) clearSelection(); });
  detail.querySelector('button').addEventListener('click', () => { clearSelection(); container.focus(); });
  function refreshZoom() {
    const percent = cy.zoom() * 100;
    document.querySelector('.zoom-controls > span').textContent = `${percent < 1 ? percent.toFixed(1) : Math.round(percent)}%`;
    document.querySelector('[data-zoom="out"]').disabled = cy.zoom() <= cy.minZoom();
    document.querySelector('[data-zoom="in"]').disabled = cy.zoom() >= cy.maxZoom();
  }
  function zoomBy(factor) {
    cy.zoom({ level: Math.min(cy.maxZoom(), Math.max(cy.minZoom(), cy.zoom() * factor)),
      renderedPosition: { x: container.clientWidth / 2, y: container.clientHeight / 2 } });
  }
  controls.forEach(button => button.addEventListener('click', () => {
    if (button.dataset.zoom === 'fit') cy.fit(undefined, 40);
    else zoomBy(button.dataset.zoom === 'in' ? 1.3 : 1 / 1.3);
  }));
  cy.on('zoom', refreshZoom);
  refreshZoom();
  container.addEventListener('keydown', event => {
    const moves = { ArrowLeft: { x: 50, y: 0 }, ArrowRight: { x: -50, y: 0 }, ArrowUp: { x: 0, y: 50 }, ArrowDown: { x: 0, y: -50 } };
    if (moves[event.key]) { event.preventDefault(); cy.panBy(moves[event.key]); }
    else if (event.key === '+' || event.key === '=') { event.preventDefault(); zoomBy(1.3); }
    else if (event.key === '-') { event.preventDefault(); zoomBy(1 / 1.3); }
    else if (event.key === 'Escape') clearSelection();
  });
  const observer = new ResizeObserver(() => cy.resize());
  observer.observe(container);
  window.addEventListener('pagehide', event => { if (!event.persisted) { observer.disconnect(); cy.destroy(); } });
})();
