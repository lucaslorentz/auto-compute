import {
  Background,
  Controls,
  Edge,
  Node,
  NodeMouseHandler,
  ReactFlowInstance,
  ReactFlow,
  useEdgesState,
  useNodesState,
} from "@xyflow/react";
import "@xyflow/react/dist/style.css";
import dagre from "dagre";
import { toPng, toSvg } from "html-to-image";
import React, {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import type { FlowGraphModel } from "../models";
import { downloadBlob } from "../utils";

interface Props {
  graph: FlowGraphModel;
}

const nodeWidth = 220;
const nodeMinHeight = 100;
type FlowNodeData = FlowGraphModel["nodes"][number]["data"];

const COLORS = {
  blue: "#1976d2",
  blueLight: "#e3f2fd",
  amber: "#d28e19",
  amberLight: "#fffcf7",
  red: "#d32f2f",
  redLight: "#ffebee",
  gray: "#78909c",
  grayLight: "#eceff1",
  white: "#fff",
  textMuted: "#4c4c4c",
  panelBorder: "#ddd",
  panelBg: "rgba(255,255,255,0.92)",
};

const propagationTargetLabels: Record<string, string> = {
  AllEntities: "All Entities",
  LoadedEntities: "Loaded Entities",
};

function getPropagationTargetLabel(propagationTarget: string) {
  return propagationTargetLabels[propagationTarget] ?? propagationTarget;
}

function getNodeColors(data: FlowNodeData) {
  if (!data.isTrackingChanges) {
    return {
      border: COLORS.red,
      background: COLORS.redLight,
    };
  }

  return {
    border: COLORS.blue,
    background: COLORS.blueLight,
  };
}

function ObservingBlock({
  observing,
  entityType,
  expandAll,
}: {
  observing?: string[] | null;
  entityType: string;
  expandAll: boolean;
}) {
  const [expanded, setExpanded] = useState(false);

  if (!observing || observing.length === 0) {
    return null;
  }

  const maxCollapsedItems = 3;
  const hiddenCount = Math.max(0, observing.length - maxCollapsedItems);
  const isExpanded = expandAll || expanded;
  const displayedItems = isExpanded
    ? observing
    : observing.slice(0, maxCollapsedItems);

  return (
    <>
      <br />
      <b>Observed members:</b>{" "}
      {displayedItems.map((member, i) => (
        <span key={member}>
          {i > 0 && ", "}
          <a
            href={`#/${encodeURIComponent(entityType)}/schema#${encodeURIComponent(member)}`}
            target="_blank"
            rel="noreferrer"
            style={{ color: COLORS.blue, textDecoration: "none" }}
          >
            {member}
          </a>
        </span>
      ))}
      {!expandAll && hiddenCount > 0 && (
        <>
          {!isExpanded && <> +{hiddenCount} more</>}{" "}
          <button
            type="button"
            onClick={() => setExpanded((v) => !v)}
            style={{
              border: "none",
              background: "transparent",
              color: COLORS.blue,
              cursor: "pointer",
              padding: 0,
              fontSize: "inherit",
            }}
          >
            {isExpanded ? "Show less" : "Show more"}
          </button>
        </>
      )}
    </>
  );
}

function NodeLabel({
  data,
  expandAllObserving,
}: {
  data: FlowNodeData;
  expandAllObserving: boolean;
}) {
  return (
    <div style={{ padding: "6px 8px", textAlign: "left", cursor: "grab" }}>
      <div
        style={{
          fontWeight: "bold",
          borderBottom: "1px solid #ccc",
          marginBottom: 2,
          fontSize: "0.85em",
        }}
      >
        <a
          href={`#/${encodeURIComponent(data.entityType)}/schema`}
          target="_blank"
          rel="noreferrer"
          style={{
            color: "inherit",
            textDecoration: "none",
          }}
          onMouseOver={(e) =>
            (e.currentTarget.style.textDecoration = "underline")
          }
          onMouseOut={(e) => (e.currentTarget.style.textDecoration = "none")}
        >
          {data.entityType.split(".").pop()}
        </a>
      </div>
      <div
        style={{
          fontSize: "0.7em",
          lineHeight: "1.3em",
          overflowWrap: "anywhere",
          wordBreak: "break-word",
        }}
      >
        <b>Type:</b> {data.label}
        {data.isTrackingChanges ? (
          <ObservingBlock
            observing={data.observing ?? []}
            entityType={data.entityType}
            expandAll={expandAllObserving}
          />
        ) : (
          <>
            <br />
            <b>Observed members:</b>{" "}
            <b style={{ color: "#d32f2f" }}>untracked</b>
          </>
        )}
      </div>
    </div>
  );
}

const getLayoutedElements = (
  nodes: Node[],
  edges: Edge[],
  getNodeHeight: (id: string) => number,
) => {
  const dagreGraph = (dagre as any).graphlib
    ? new (dagre as any).graphlib.Graph()
    : new (dagre as any).Graph();
  dagreGraph.setDefaultEdgeLabel(() => ({}));
  dagreGraph.setGraph({ rankdir: "TD", nodesep: 40, ranksep: 70 });

  nodes.forEach((node) => {
    const nodeHeight = getNodeHeight(node.id);
    dagreGraph.setNode(node.id, { width: nodeWidth, height: nodeHeight });
  });

  edges.forEach((edge) => {
    dagreGraph.setEdge(edge.source, edge.target);
  });

  dagre.layout(dagreGraph);

  nodes.forEach((node) => {
    const nodeWithPosition = dagreGraph.node(node.id);
    const nodeHeight = getNodeHeight(node.id);
    node.position = {
      x: nodeWithPosition.x - nodeWidth / 2,
      y: nodeWithPosition.y - nodeHeight / 2,
    };
  });

  return { nodes, edges };
};

function collectConnectedNodeIds(
  focusNodeId: string,
  edges: { source: string; target: string }[],
) {
  const forward = new Map<string, string[]>();
  const backward = new Map<string, string[]>();

  for (const edge of edges) {
    if (!forward.has(edge.source)) {
      forward.set(edge.source, []);
    }
    if (!backward.has(edge.target)) {
      backward.set(edge.target, []);
    }

    forward.get(edge.source)!.push(edge.target);
    backward.get(edge.target)!.push(edge.source);
  }

  const visited = new Set<string>([focusNodeId]);
  const queue = [focusNodeId];

  while (queue.length > 0) {
    const current = queue.shift()!;
    const next = [
      ...(forward.get(current) ?? []),
      ...(backward.get(current) ?? []),
    ];

    for (const candidate of next) {
      if (visited.has(candidate)) {
        continue;
      }
      visited.add(candidate);
      queue.push(candidate);
    }
  }

  return visited;
}

async function waitForFlowRender(
  container: HTMLElement,
  expectedNodeCount: number,
  expectedEdgeCount: number,
) {
  const timeoutMs = 2500;
  const start = Date.now();

  while (Date.now() - start < timeoutMs) {
    const renderedNodes =
      container.querySelectorAll(".react-flow__node").length;
    const renderedEdges = container.querySelectorAll(
      ".react-flow__edges path.react-flow__edge-path",
    ).length;
    if (
      renderedNodes >= expectedNodeCount &&
      renderedEdges >= expectedEdgeCount
    ) {
      return;
    }

    await new Promise((resolve) =>
      requestAnimationFrame(() => resolve(undefined)),
    );
  }
}

export function DependencyFlow({ graph }: Props) {
  const [nodes, setNodes, onNodesChange] = useNodesState<Node>([] as any);
  const [edges, setEdges, onEdgesChange] = useEdgesState<Edge>([] as any);
  const [focusedNodeId, setFocusedNodeId] = useState<string | null>(null);
  const [expandAllObserving, setExpandAllObserving] = useState(false);
  const [isCapturingImage, setIsCapturingImage] = useState(false);
  const [reactFlowInstance, setReactFlowInstance] =
    useState<ReactFlowInstance | null>(null);
  const containerRef = useRef<HTMLDivElement>(null);
  const legendRef = useRef<HTMLDivElement>(null);
  const measuredHeightsRef = useRef<Record<string, number>>({});

  const handleDownloadImage = useCallback(
    async (format: "png" | "svg") => {
      if (!containerRef.current || !reactFlowInstance) {
        return;
      }

      if (nodes.length === 0) {
        return;
      }

      const padding = 40;
      const bounds = nodes.reduce(
        (acc, node) => {
          const nodeHeight =
            measuredHeightsRef.current[node.id] ?? nodeMinHeight;
          const left = node.position.x;
          const top = node.position.y;
          const right = left + nodeWidth;
          const bottom = top + nodeHeight;

          return {
            minX: Math.min(acc.minX, left),
            minY: Math.min(acc.minY, top),
            maxX: Math.max(acc.maxX, right),
            maxY: Math.max(acc.maxY, bottom),
          };
        },
        {
          minX: Number.POSITIVE_INFINITY,
          minY: Number.POSITIVE_INFINITY,
          maxX: Number.NEGATIVE_INFINITY,
          maxY: Number.NEGATIVE_INFINITY,
        },
      );

      const legendWidth = legendRef.current?.offsetWidth ?? 240;
      const legendHeight = legendRef.current?.offsetHeight ?? 120;
      const legendLeft = 8;
      const legendTop = 8;
      const legendGap = 12;

      const baseViewport = {
        x: -bounds.minX + padding,
        y: -bounds.minY + padding,
        zoom: 1,
      };

      let exportOffsetX = 0;
      let exportOffsetY = 0;

      const legendRect = {
        left: legendLeft,
        top: legendTop,
        right: legendLeft + legendWidth,
        bottom: legendTop + legendHeight,
      };

      const projectedNodeRects = nodes.map((node) => {
        const nodeHeight = measuredHeightsRef.current[node.id] ?? nodeMinHeight;
        const left = node.position.x + baseViewport.x;
        const top = node.position.y + baseViewport.y;

        return {
          left,
          top,
          right: left + nodeWidth,
          bottom: top + nodeHeight,
        };
      });

      const overlapsLegend = (rect: {
        left: number;
        top: number;
        right: number;
        bottom: number;
      }) =>
        rect.left < legendRect.right &&
        rect.right > legendRect.left &&
        rect.top < legendRect.bottom &&
        rect.bottom > legendRect.top;

      const overlappingNodeRects = projectedNodeRects.filter(overlapsLegend);
      if (overlappingNodeRects.length > 0) {
        let requiredShiftX = 0;
        let requiredShiftY = 0;

        for (const rect of overlappingNodeRects) {
          const verticalOverlap =
            rect.bottom > legendRect.top && rect.top < legendRect.bottom;
          const horizontalOverlap =
            rect.right > legendRect.left && rect.left < legendRect.right;

          if (verticalOverlap) {
            requiredShiftX = Math.max(
              requiredShiftX,
              legendRect.right + legendGap - rect.left,
            );
          }

          if (horizontalOverlap) {
            requiredShiftY = Math.max(
              requiredShiftY,
              legendRect.bottom + legendGap - rect.top,
            );
          }
        }

        if (requiredShiftX > 0 && requiredShiftY > 0) {
          if (requiredShiftX <= requiredShiftY) {
            exportOffsetX = requiredShiftX;
          } else {
            exportOffsetY = requiredShiftY;
          }
        } else {
          exportOffsetX = requiredShiftX;
          exportOffsetY = requiredShiftY;
        }
      }

      const captureWidth = Math.max(
        1,
        Math.ceil(bounds.maxX - bounds.minX + padding * 2 + exportOffsetX),
        Math.ceil(legendRect.right + padding),
      );
      const captureHeight = Math.max(
        1,
        Math.ceil(bounds.maxY - bounds.minY + padding * 2 + exportOffsetY),
        Math.ceil(legendRect.bottom + padding),
      );
      const exportViewport = {
        x: baseViewport.x + exportOffsetX,
        y: baseViewport.y + exportOffsetY,
        zoom: 1,
      };
      const previousViewport = reactFlowInstance.getViewport();
      const previousWidth = containerRef.current.style.width;
      const previousHeight = containerRef.current.style.height;
      const previousPosition = containerRef.current.style.position;
      const previousLeft = containerRef.current.style.left;
      const previousTop = containerRef.current.style.top;
      const previousZIndex = containerRef.current.style.zIndex;
      const previousBackground = containerRef.current.style.background;
      try {
        setIsCapturingImage(true);
        containerRef.current.style.position = "fixed";
        containerRef.current.style.left = "0";
        containerRef.current.style.top = "0";
        containerRef.current.style.zIndex = "9999";
        containerRef.current.style.background = COLORS.white;
        containerRef.current.style.width = `${captureWidth}px`;
        containerRef.current.style.height = `${captureHeight}px`;
        await reactFlowInstance.setViewport(exportViewport, { duration: 0 });
        await new Promise((resolve) =>
          requestAnimationFrame(() => resolve(undefined)),
        );
        await new Promise((resolve) =>
          requestAnimationFrame(() => resolve(undefined)),
        );
        await waitForFlowRender(
          containerRef.current,
          graph.nodes.length,
          graph.edges.length,
        );

        const dataUrl =
          format === "svg"
            ? await toSvg(containerRef.current, {
                backgroundColor: COLORS.white,
                cacheBust: true,
              })
            : await toPng(containerRef.current, {
                backgroundColor: COLORS.white,
                cacheBust: true,
                pixelRatio: 2,
              });
        const blob = await (await fetch(dataUrl)).blob();
        downloadBlob(
          blob,
          format === "svg" ? "dependency-graph.svg" : "dependency-graph.png",
        );
      } finally {
        setIsCapturingImage(false);
        containerRef.current.style.position = previousPosition;
        containerRef.current.style.left = previousLeft;
        containerRef.current.style.top = previousTop;
        containerRef.current.style.zIndex = previousZIndex;
        containerRef.current.style.background = previousBackground;
        containerRef.current.style.width = previousWidth;
        containerRef.current.style.height = previousHeight;
        await reactFlowInstance.setViewport(previousViewport, { duration: 0 });
      }
    },
    [graph.edges.length, graph.nodes.length, nodes, reactFlowInstance],
  );

  const focusRelatedNodeIds = useMemo(() => {
    if (!focusedNodeId) {
      return null;
    }

    const containsFocused = graph.nodes.some((n) => n.id === focusedNodeId);
    if (!containsFocused) {
      return null;
    }

    return collectConnectedNodeIds(focusedNodeId, graph.edges);
  }, [graph.edges, graph.nodes, focusedNodeId]);

  useEffect(() => {
    const nodeDataByNodeId = new Map(graph.nodes.map((n) => [n.id, n.data]));

    const initialNodes: Node[] = graph.nodes.map((n) => {
      const colors = getNodeColors(n.data);
      const isFocused = !focusRelatedNodeIds || focusRelatedNodeIds.has(n.id);

      return {
        id: n.id,
        data: {
          label: (
            <NodeLabel data={n.data} expandAllObserving={expandAllObserving} />
          ),
        },
        position: { x: 0, y: 0 },
        style: {
          width: nodeWidth,
          padding: 0,
          background: colors.background,
          border: `1px solid ${colors.border}`,
          borderRadius: 4,
          boxShadow: "0 1px 3px rgba(0,0,0,0.1)",
          opacity: isFocused ? 1 : 0.25,
        },
      };
    });

    const initialEdges: Edge[] = graph.edges.map((e) => {
      const targetData = nodeDataByNodeId.get(e.target);
      const isFocused =
        !focusRelatedNodeIds ||
        (focusRelatedNodeIds.has(e.source) &&
          focusRelatedNodeIds.has(e.target));

      return {
        id: e.id,
        source: e.source,
        target: e.target,
        animated: !isCapturingImage,
        label: e.label,
        labelShowBg: true,
        labelBgStyle: { fill: COLORS.white, fillOpacity: 0.9 },
        labelBgPadding: [4, 2],
        labelStyle: { fontSize: 11, fill: COLORS.textMuted },
        style: {
          strokeWidth: 2,
          strokeDasharray: "6 4",
          stroke:
            targetData?.propagationTarget === "LoadedEntities"
              ? COLORS.amber
              : "#555",
          animationDirection: "reverse",
          opacity: isFocused ? 1 : 0.2,
        },
      };
    });

    const { nodes: layoutedNodes, edges: layoutedEdges } = getLayoutedElements(
      initialNodes,
      initialEdges,
      (id) => measuredHeightsRef.current[id] ?? nodeMinHeight,
    );

    setNodes([...layoutedNodes] as any);
    setEdges([...layoutedEdges] as any);
  }, [
    expandAllObserving,
    graph,
    focusRelatedNodeIds,
    isCapturingImage,
    setEdges,
    setNodes,
  ]);

  useEffect(() => {
    if (nodes.length === 0 || !containerRef.current) {
      return;
    }

    const handle = requestAnimationFrame(() => {
      const nextHeights: Record<string, number> = {};
      let hasChanged = false;

      for (const node of nodes) {
        const element = containerRef.current?.querySelector(
          `.react-flow__node[data-id="${node.id}"]`,
        ) as HTMLElement | null;
        const measuredHeight = element?.offsetHeight;

        if (!measuredHeight) {
          continue;
        }

        nextHeights[node.id] = measuredHeight;

        if (
          Math.abs(
            (measuredHeightsRef.current[node.id] ?? 0) - measuredHeight,
          ) > 1
        ) {
          hasChanged = true;
        }
      }

      if (!hasChanged) {
        return;
      }

      measuredHeightsRef.current = {
        ...measuredHeightsRef.current,
        ...nextHeights,
      };

      const { nodes: layoutedNodes, edges: layoutedEdges } =
        getLayoutedElements(
          nodes.map((n) => ({ ...n })),
          edges.map((e) => ({ ...e })),
          (id) => measuredHeightsRef.current[id] ?? nodeMinHeight,
        );

      setNodes([...layoutedNodes] as any);
      setEdges([...layoutedEdges] as any);
    });

    return () => cancelAnimationFrame(handle);
  }, [edges, nodes, setEdges, setNodes]);

  const onNodeClick = useMemo<NodeMouseHandler>(
    () => (_event, node) => {
      setFocusedNodeId((current) => (current === node.id ? null : node.id));
    },
    [],
  );

  return (
    <div
      ref={containerRef}
      style={{
        width: "100%",
        height: "100%",
        border: `1px solid ${COLORS.panelBorder}`,
        position: "relative",
      }}
    >
      <div
        ref={legendRef}
        style={{
          position: "absolute",
          top: 8,
          left: 8,
          zIndex: 10,
          background: COLORS.panelBg,
          border: `1px solid ${COLORS.panelBorder}`,
          borderRadius: 6,
          padding: "8px 10px",
          fontSize: 12,
          display: "grid",
          gap: 6,
          minWidth: 230,
        }}
      >
        <b>Nodes</b>
        <span>
          <span
            style={{
              display: "inline-block",
              width: 10,
              height: 10,
              background: COLORS.blueLight,
              border: `1px solid ${COLORS.blue}`,
              borderRadius: 2,
              marginRight: 6,
            }}
          />
          Tracking changes
        </span>
        <span>
          <span
            style={{
              display: "inline-block",
              width: 10,
              height: 10,
              background: COLORS.redLight,
              border: `1px solid ${COLORS.red}`,
              borderRadius: 2,
              marginRight: 6,
            }}
          />
          Not tracking changes
        </span>
        <b style={{ marginTop: 4 }}>Edges</b>
        <span>
          <span
            style={{
              display: "inline-block",
              width: 10,
              height: 0,
              borderTop: "2px dashed #555",
              marginRight: 6,
              verticalAlign: "middle",
            }}
          />
          Propagates to all entities
        </span>
        <span>
          <span
            style={{
              display: "inline-block",
              width: 10,
              height: 0,
              borderTop: `2px dashed ${COLORS.amber}`,
              marginRight: 6,
              verticalAlign: "middle",
            }}
          />
          Propagates to loaded entities
        </span>
        {!isCapturingImage && (
          <>
            <b style={{ marginTop: 4 }}>Options</b>
            <label
              style={{ display: "inline-flex", alignItems: "center", gap: 6 }}
            >
              <input
                type="checkbox"
                checked={expandAllObserving}
                onChange={(e) => setExpandAllObserving(e.target.checked)}
              />
              Expand all
            </label>
            <div
              style={{ display: "inline-flex", alignItems: "center", gap: 6 }}
            >
              <span style={{ fontSize: 11, color: COLORS.textMuted }}>
                Download:
              </span>
              <button
                type="button"
                onClick={() => handleDownloadImage("png")}
                style={{
                  border: `1px solid ${COLORS.panelBorder}`,
                  borderRadius: 4,
                  background: COLORS.white,
                  cursor: "pointer",
                  padding: "3px 6px",
                  fontSize: 11,
                  lineHeight: 1.1,
                }}
              >
                PNG
              </button>
              <button
                type="button"
                onClick={() => handleDownloadImage("svg")}
                style={{
                  border: `1px solid ${COLORS.panelBorder}`,
                  borderRadius: 4,
                  background: COLORS.white,
                  cursor: "pointer",
                  padding: "3px 6px",
                  fontSize: 11,
                  lineHeight: 1.1,
                }}
              >
                SVG
              </button>
            </div>
          </>
        )}
      </div>

      <ReactFlow
        nodes={nodes}
        edges={edges}
        onInit={setReactFlowInstance}
        onNodesChange={onNodesChange}
        onEdgesChange={onEdgesChange}
        onNodeClick={onNodeClick}
        fitView
        nodesDraggable={false}
        nodesConnectable={false}
        elementsSelectable
        zoomOnScroll={false}
        panOnScroll
        preventScrolling={false}
      >
        <Background />
        {!isCapturingImage && <Controls showInteractive={false} />}
      </ReactFlow>
    </div>
  );
}
