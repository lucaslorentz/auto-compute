// TypeScript models matching the C# DTOs

export interface EntModel {
  name: string;
}

export interface EntDetailsModel {
  name: string;
  properties: EntPropertyModel[];
  navigations: EntNavigationModel[];
  methods: EntMethodModel[];
  observers: EntObserverModel[];
}

export interface EntPropertyModel {
  name: string;
  isPrimaryKey: boolean;
  isShadow: boolean;
  clrType: string;
  computed: EntComputedModel | null;
  enumItems: Record<string, EntEnumItemModel> | null;
}

export interface EntEnumItemModel {
  value: string;
  label: string;
}

export interface EntNavigationModel {
  name: string;
  isCollection: boolean;
  targetEntity: string;
  filterKey: string | null;
  computed: EntComputedModel | null;
}

export interface EntMethodModel {
  name: string;
  clrType: string;
  enumItems: Record<string, EntEnumItemModel> | null;
}

export interface EntComputedModel {
  name: string;
  entity: string;
  member: string;
  expression: string;
  dependencies: EntObservedMemberModel[] | null;
  allEntitiesDependencies: EntObservedMemberModel[] | null;
  loadedEntitiesDependencies: EntObservedMemberModel[] | null;
  entityContextGraph: FlowGraphModel;
}

export interface EntObserverModel {
  name: string;
  entity: string;
  expression: string;
  dependencies: EntObservedMemberModel[] | null;
  allEntitiesDependencies: EntObservedMemberModel[] | null;
  loadedEntitiesDependencies: EntObservedMemberModel[] | null;
  entityContextGraph: FlowGraphModel;
}

export interface EntObservedMemberModel {
  entityName: string;
  memberName: string;
}

export interface EntDataModel {
  id: unknown;
  propertyValues: Record<string, unknown>;
  referenceValues: Record<string, EntityReferenceModel | null>;
  computedValues: Record<string, unknown>;
  methodValues: Record<string, unknown>;
  membersConsistency: Record<string, unknown>;
}

export interface EntListModel {
  entities: EntDataModel[];
  nextPageToken: unknown;
  hasNextPage: boolean;
}

export interface ConsistencyModel {
  consistentCount: number;
  inconsistentCount: number;
  totalCount: number;
  consistencyPercentage: number;
}

export interface EntityReferenceModel {
  id?: unknown;
  toStringValue?: string | null;
  count?: number | null;
}

export interface FlowGraphModel {
  nodes: FlowNodeModel[];
  edges: FlowEdgeModel[];
}

export interface FlowNodeModel {
  id: string;
  type: string;
  data: FlowNodeDataModel;
}

export interface FlowNodeDataModel {
  label: string;
  entityType: string;
  expression: string;
  isTrackingChanges: boolean;
  propagationTarget: string;
  canResolveLoadedEntities: boolean;
  observing?: string[] | null;
}

export interface FlowEdgeModel {
  id: string;
  source: string;
  target: string;
  label?: string | null;
}
