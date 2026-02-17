import ArrowBackIosNewIcon from "@mui/icons-material/ArrowBackIosNew";
import ArrowForwardIosIcon from "@mui/icons-material/ArrowForwardIos";
import CheckCircleIcon from "@mui/icons-material/CheckCircle";
import RefreshIcon from "@mui/icons-material/Refresh";
import SettingsIcon from "@mui/icons-material/Settings";
import WarningIcon from "@mui/icons-material/Warning";
import {
  Box,
  Breadcrumbs,
  Button,
  Checkbox,
  Divider,
  Fab,
  IconButton,
  LinearProgress,
  Link,
  ListSubheader,
  Menu,
  MenuItem,
  Paper,
  Select,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from "@mui/material";
import {
  parseAsBoolean,
  parseAsIsoDate,
  parseAsJson,
  parseAsString,
  useQueryStates,
} from "nuqs";
import React, { useEffect, useRef, useState } from "react";
import { useQuery } from "react-query";
import { NavLink, useParams } from "react-router-dom";
import { apiFetch } from "../api";
import type { EntDetailsModel, EntListModel } from "../models";
import { ColumnHeaderFilter } from "./ColumnHeaderFilter";
import { NavigationFilterAutocomplete } from "./NavigationFilterAutocomplete";
import { NavigationRenderer } from "./NavigationRenderer";
import { PropertyFilterInput } from "./PropertyFilterInput";
import { ValueRenderer } from "./ValueRenderer";

export function EntityItemList() {
  const { name } = useParams();

  if (!name) throw new Error("Name not informed");

  const [rowsPerPage, setRowsPerPage] = useState(10);

  const [query, setQuery] = useQueryStates({
    include: parseAsString.withDefault("Id,ToString()"),
    inconsistentMember: parseAsString,
    since: parseAsIsoDate,
    sortBy: parseAsString,
    sortDescending: parseAsBoolean.withDefault(false),
    columnFilters: parseAsJson<Record<string, unknown>>((v) => {
      // Revive ISO date strings back to Date objects
      const record = v as Record<string, unknown>;
      const ISO_DATE_RE = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}/;
      for (const key of Object.keys(record)) {
        const val = record[key];
        if (typeof val === "string" && ISO_DATE_RE.test(val)) {
          const d = new Date(val);
          if (!isNaN(d.getTime())) record[key] = d;
        }
      }
      return record;
    }).withDefault({}),
  });

  const {
    include: includeStr,
    inconsistentMember,
    since,
    sortBy,
    sortDescending,
    columnFilters: columnFiltersUrl,
  } = query;

  const include = includeStr.split(",");

  const setInclude = (next: string[]) => {
    setQuery({ include: next.join(",") });
  };


  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const [columnSearch, setColumnSearch] = useState("");

  const [columnFilters, setColumnFiltersLocal] = useState<Record<string, unknown>>(columnFiltersUrl);
  const columnFiltersDebounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const columnFiltersPendingRef = useRef<Record<string, unknown> | null>(null);
  const openFilterMenuRef = useRef(false);

  // Sync local state from URL when there's no pending local change
  useEffect(() => {
    if (columnFiltersPendingRef.current === null) {
      setColumnFiltersLocal(columnFiltersUrl);
    }
  }, [columnFiltersUrl]);

  const flushColumnFilters = (next: Record<string, unknown>) => {
    if (columnFiltersDebounceRef.current) clearTimeout(columnFiltersDebounceRef.current);
    columnFiltersPendingRef.current = null;
    setQuery({ columnFilters: next });
  };

  const setColumnFilters = (updater: ((prev: Record<string, unknown>) => Record<string, unknown>) | Record<string, unknown>) => {
    setColumnFiltersLocal((prev) => {
      const next = typeof updater === "function" ? updater(prev) : updater;
      if (columnFiltersDebounceRef.current) clearTimeout(columnFiltersDebounceRef.current);
      columnFiltersPendingRef.current = next;
      columnFiltersDebounceRef.current = setTimeout(() => {
        if (!openFilterMenuRef.current) {
          columnFiltersPendingRef.current = null;
          setQuery({ columnFilters: next });
        }
      }, 500);
      return next;
    });
  };

  const clearColumnFilters = (updater: (prev: Record<string, unknown>) => Record<string, unknown>) => {
    if (columnFiltersDebounceRef.current) clearTimeout(columnFiltersDebounceRef.current);
    columnFiltersPendingRef.current = null;
    setColumnFiltersLocal((prev) => {
      const next = updater(prev);
      setQuery({ columnFilters: next });
      return next;
    });
  };


  const [pageToken, setPageToken] = useState<string | null>(null);
  const [tokensHistory, setTokensHistory] = useState<(string | null)[]>([]);

  const handleMenuOpen = (event: React.MouseEvent<HTMLElement>) => {
    setAnchorEl(event.currentTarget);
  };

  const handleMenuClose = () => {
    setAnchorEl(null);
    setColumnSearch("");
  };

  const toggleInclude = (id: string) => {
    const removing = include.includes(id);
    const nextInclude = removing
      ? include.filter((x) => x !== id)
      : [...include, id];
    setInclude(nextInclude);
    if (removing) {
      clearColumnFilters((prev) => {
        const next = { ...prev };
        delete next[id];
        delete next[`${id}_gte`];
        delete next[`${id}_lte`];
        return next;
      });
      if (inconsistentMember === id) setQuery({ inconsistentMember: null });
    }
  };

  const includeCommaSeparated = includeStr;

  const entSchemaQuery = useQuery(["ents", name], {
    async queryFn() {
      return await apiFetch<EntDetailsModel>(`/ents/${name}`);
    },
  });

  const entListQuery = useQuery(
    [
      "ents",
      name,
      "items",
      {
        includeCommaSeparated,
        inconsistentMember,
        since,
        pageToken,
        rowsPerPage,
        sortBy,
        sortDescending,
        columnFilters: columnFiltersUrl,
      },
    ],
    {
      async queryFn() {
        const params = new URLSearchParams();
        if (includeCommaSeparated) params.append("include", includeCommaSeparated);
        if (inconsistentMember) params.append("inconsistentMember", inconsistentMember);
        params.append("pageSize", String(rowsPerPage));
        if (pageToken) params.append("pageToken", pageToken);
        if (since && inconsistentMember) params.append("since", since.toJSON());
        if (sortBy) {
          params.append("sortBy", sortBy);
          params.append("sortDescending", String(sortDescending));
        }
        Object.entries(columnFiltersUrl).forEach(([key, value]) => {
          if (value)
            params.append(
              `f_${key}`,
              value instanceof Date ? value.toJSON() : String(value)
            );
        });
        return await apiFetch<EntListModel>(`/ents/${name}/items?${params.toString()}`);
      },
      keepPreviousData: true,
    }
  );

  const entSchema = entSchemaQuery.data;
  const entList = entListQuery.data;

  useEffect(() => {
    if (include.length === 0) {
      setInclude(["Id", "ToString()"]);
    }
  }, [includeStr]);

  useEffect(() => {
    setPageToken(null);
    setTokensHistory([]);
  }, [name, inconsistentMember, since, rowsPerPage, columnFiltersUrl, sortBy, sortDescending]);

  const handleSort = (property: string | null, descending: boolean) => {
    setQuery({
      sortBy: property,
      sortDescending: property === null ? null : descending,
    });
  };

  const handleNextPage = () => {
    if (entList?.nextPageToken) {
      setTokensHistory([...tokensHistory, pageToken]);
      setPageToken(entList.nextPageToken as string);
    }
  };

  const handlePreviousPage = () => {
    if (tokensHistory.length > 0) {
      const prevToken = tokensHistory[tokensHistory.length - 1];
      setTokensHistory(tokensHistory.slice(0, -1));
      setPageToken(prevToken);
    }
  };

  if (!entSchema) return <LinearProgress />;

  const includedProperties = entSchema.properties.filter((p) => include.includes(p.name));
  const includedNavigations = entSchema.navigations.filter((n) => include.includes(n.name));
  const includedMethods = entSchema.methods.filter((m) => include.includes(m.name));

  return (
    <Stack padding={3} spacing={2}>
      <Stack direction="row" justifyContent="space-between" alignItems="center">
        <Stack spacing={1}>
          <Breadcrumbs aria-label="breadcrumb">
            <Link component={NavLink} to="/" color="inherit">
              Entities
            </Link>
            <Link component={NavLink} to={`/${encodeURIComponent(name)}/schema`} color="inherit">
              {entSchema.name}
            </Link>
          </Breadcrumbs>
          <Typography variant="h4" fontWeight="bold">
            Items
          </Typography>
        </Stack>

        <Stack direction="row" spacing={2} alignItems="center">
          <Fab
            size="small"
            onClick={handleMenuOpen}
            sx={{
              bgcolor: "white",
              color: "text.secondary",
              boxShadow: "none",
              border: "1px solid rgba(0,0,0,0.12)",
              "&:hover": { bgcolor: "#f5f5f5" },
            }}
          >
            <SettingsIcon fontSize="small" />
          </Fab>

          <Menu
            anchorEl={anchorEl}
            open={Boolean(anchorEl)}
            onClose={handleMenuClose}
            MenuListProps={{ disablePadding: true }}
            PaperProps={{ sx: { maxHeight: 400, width: 300 } }}
          >
            <Box
              sx={{
                p: 1,
                position: "sticky",
                top: 0,
                bgcolor: "white",
                zIndex: 2,
              }}
            >
              <TextField
                size="small"
                fullWidth
                placeholder="Search columns..."
                value={columnSearch}
                onChange={(e) => setColumnSearch(e.target.value)}
                onKeyDown={(e) => e.stopPropagation()}
                inputRef={(e) => setTimeout(() => e?.focus(), 0)}
              />
              <Button
                size="small"
                fullWidth
                sx={{ mt: 0.5 }}
                onClick={() => setInclude(["Id", "ToString()"])}
              >
                Reset to default
              </Button>
            </Box>
            <Divider />
            {entSchema.properties.filter((p) =>
              p.name.toLowerCase().includes(columnSearch.toLowerCase())
            ).length > 0 && (
                <>
                  <ListSubheader disableSticky>Properties</ListSubheader>
                  {entSchema.properties
                    .filter((p) => p.name.toLowerCase().includes(columnSearch.toLowerCase()))
                    .map((property) => (
                      <MenuItem key={property.name} sx={{ py: 0 }} onClick={() => toggleInclude(property.name)}>
                        <Checkbox size="small" checked={include.includes(property.name)} />
                        <Typography variant="body2">{property.name}</Typography>
                      </MenuItem>
                    ))}
                </>
              )}
            {entSchema.navigations.filter((n) =>
              n.name.toLowerCase().includes(columnSearch.toLowerCase())
            ).length > 0 && (
                <>
                  <ListSubheader disableSticky>Navigations</ListSubheader>
                  {entSchema.navigations
                    .filter((n) => n.name.toLowerCase().includes(columnSearch.toLowerCase()))
                    .map((nav) => (
                      <MenuItem key={nav.name} sx={{ py: 0 }} onClick={() => toggleInclude(nav.name)}>
                        <Checkbox size="small" checked={include.includes(nav.name)} />
                        <Typography variant="body2">{nav.name}</Typography>
                      </MenuItem>
                    ))}
                </>
              )}
            {entSchema.methods.filter((m) =>
              m.name.toLowerCase().includes(columnSearch.toLowerCase())
            ).length > 0 && (
                <>
                  <ListSubheader disableSticky>Methods</ListSubheader>
                  {entSchema.methods
                    .filter((m) => m.name.toLowerCase().includes(columnSearch.toLowerCase()))
                    .map((method) => (
                      <MenuItem key={method.name} sx={{ py: 0 }} onClick={() => toggleInclude(method.name)}>
                        <Checkbox size="small" checked={include.includes(method.name)} />
                        <Typography variant="body2">{method.name}</Typography>
                      </MenuItem>
                    ))}
                </>
              )}
          </Menu>
        </Stack>
      </Stack>

      <Paper variant="outlined" sx={{ overflow: "hidden" }}>
        {entListQuery.isFetching && <LinearProgress />}
        <TableContainer>
          <Table stickyHeader sx={{ tableLayout: "fixed", lineBreak: "anywhere", "& .MuiTableCell-stickyHeader": { backgroundColor: "#f5f5f5" } }}>
            <TableHead>
              <TableRow>
                {includedProperties.map((property) => {
                  const exactValue = columnFilters[property.name];
                  const gte = columnFilters[`${property.name}_gte`];
                  const lte = columnFilters[`${property.name}_lte`];
                  const isDateOnly = property.clrType.toLowerCase().includes("dateonly");

                  function formatValue(val: unknown): string {
                    if (property.enumItems) {
                      return property.enumItems[String(val)]?.label || String(val);
                    } else if (val instanceof Date || (typeof val === "string" && val && !isNaN(new Date(val).getTime()) && (isDateOnly || String(val).includes("T")))) {
                      const d = val instanceof Date ? val : new Date(String(val));
                      return isDateOnly
                        ? d.toISOString().split("T")[0]
                        : d.toISOString().replace("T", " ").substring(0, 19);
                    } else {
                      return String(val);
                    }
                  }

                  let activeValue: string | undefined;
                  if (exactValue) {
                    activeValue = formatValue(exactValue);
                  } else if (gte || lte) {
                    const displayGte = gte ? formatValue(gte) : "*";
                    const displayLte = lte ? formatValue(lte) : "*";
                    activeValue = `${displayGte} - ${displayLte}`;
                  }

                  return (
                    <TableCell key={property.name} sx={{ verticalAlign: "top" }}>
                      <ColumnHeaderFilter
                        label={property.name}
                        property={property.name}
                        active={!!(exactValue || gte || lte || inconsistentMember === property.name)}
                        activeValue={activeValue}
                        sortBy={sortBy}
                        sortDescending={sortDescending}
                        onSort={handleSort}
                        isComputed={!!property.computed}
                        isInconsistent={inconsistentMember === property.name}
                        onToggleInconsistent={(checked) => {
                          if (checked) {
                            const nextInclude = include.includes(property.name)
                              ? include
                              : [...include, property.name];
                            setQuery({
                              inconsistentMember: property.name,
                              include: nextInclude.join(","),
                            });
                          } else {
                            setQuery({ inconsistentMember: null, since: null });
                          }
                        }}
                        since={since}
                        onSinceChange={(newSince) => setQuery({ since: newSince })}
                        onFilterMenuOpen={() => { openFilterMenuRef.current = true; }}
                        onFilterMenuClose={() => {
                          openFilterMenuRef.current = false;
                          if (columnFiltersPendingRef.current !== null) {
                            flushColumnFilters(columnFiltersPendingRef.current);
                          }
                        }}
                        onClear={() => {
                          clearColumnFilters((prev) => {
                            const next = { ...prev };
                            delete next[property.name];
                            delete next[`${property.name}_gte`];
                            delete next[`${property.name}_lte`];
                            return next;
                          });
                          if (inconsistentMember === property.name) setQuery({ inconsistentMember: null });
                        }}
                      >
                        <PropertyFilterInput
                          property={property}
                          values={columnFilters}
                          onChange={(key, val) =>
                            setColumnFilters((prev) => ({ ...prev, [key]: val }))
                          }
                        />
                      </ColumnHeaderFilter>
                    </TableCell>
                  );
                })}
                {includedNavigations.map((nav) => (
                  <TableCell key={nav.name} sx={{ verticalAlign: "top" }}>
                    <ColumnHeaderFilter
                      label={nav.name}
                      property={nav.name}
                      active={!!columnFilters[nav.name]}
                      activeValue={String(columnFilters[nav.name])}
                      sortBy={sortBy}
                      sortDescending={sortDescending}
                      onSort={handleSort}
                      onFilterMenuOpen={() => { openFilterMenuRef.current = true; }}
                      onFilterMenuClose={() => {
                        openFilterMenuRef.current = false;
                        if (columnFiltersPendingRef.current !== null) {
                          flushColumnFilters(columnFiltersPendingRef.current);
                        }
                      }}
                      onClear={() => {
                        clearColumnFilters((prev) => {
                          const next = { ...prev };
                          delete next[nav.name];
                          return next;
                        });
                      }}
                    >
                      {!nav.isCollection && (
                        <NavigationFilterAutocomplete
                          targetEntity={nav.targetEntity}
                          value={columnFilters[nav.name] || ""}
                          onChange={(val) =>
                            setColumnFilters((prev) => ({ ...prev, [nav.name]: val }))
                          }
                        />
                      )}
                    </ColumnHeaderFilter>
                  </TableCell>
                ))}
                {includedMethods.map((method) => (
                  <TableCell key={method.name} sx={{ verticalAlign: "top" }}>
                    <ColumnHeaderFilter label={method.name} hideButton>
                      <Typography variant="subtitle2" sx={{ fontWeight: "bold" }}>
                        {method.name}
                      </Typography>
                    </ColumnHeaderFilter>
                  </TableCell>
                ))}
              </TableRow>
            </TableHead>
            <TableBody>
              {entList?.entities.map((entity, entityIndex) => (
                <TableRow hover key={entityIndex}>
                  {includedProperties.map((property) => {
                    const value = entity.propertyValues[property.name];
                    const computedValue = entity.computedValues[property.name];
                    const isConsistent = entity.membersConsistency[property.name] === true;

                    return (
                      <TableCell key={property.name}>
                        <Stack direction="row" spacing={1} alignItems="center">
                          <Box sx={{ flex: 1 }}>
                            {property.isPrimaryKey ? (
                              <Link
                                component={NavLink}
                                to={`/${encodeURIComponent(entSchema.name)}/items/${encodeURIComponent(String(entity.id))}`}
                                fontWeight={600}
                              >
                                <ValueRenderer value={value} property={property} />
                              </Link>
                            ) : (
                              <ValueRenderer value={value} property={property} />
                            )}
                          </Box>
                          {property.computed && (
                            isConsistent ? (
                              <Tooltip title="Consistent">
                                <CheckCircleIcon color="success" sx={{ fontSize: 16 }} />
                              </Tooltip>
                            ) : (
                              <Tooltip title={`Inconsistent! Computed value is: ${JSON.stringify(computedValue)}`}>
                                <WarningIcon color="warning" fontSize="small" />
                              </Tooltip>
                            )
                          )}
                        </Stack>
                      </TableCell>
                    );
                  })}
                  {includedNavigations.map((navigation) => (
                    <TableCell key={navigation.name}>
                      <NavigationRenderer
                        navigation={navigation}
                        value={entity.referenceValues[navigation.name]}
                        sourceId={entity.id}
                      />
                    </TableCell>
                  ))}
                  {includedMethods.map((method) => (
                    <TableCell key={method.name}>
                      <ValueRenderer value={entity.methodValues[method.name]} property={method} />
                    </TableCell>
                  ))}
                </TableRow>
              ))}
              {entList?.entities.length === 0 && !entListQuery.isLoading && (
                <TableRow>
                  <TableCell
                    colSpan={includedProperties.length + includedNavigations.length + includedMethods.length}
                    align="center"
                    sx={{ py: 4, color: "text.secondary" }}
                  >
                    No data found
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </TableContainer>

        <Box
          sx={{
            display: "grid",
            gridTemplateColumns: "1fr auto 1fr",
            alignItems: "center",
            p: 1,
            px: 2,
            borderTop: "1px solid rgba(0,0,0,0.12)",
          }}
        >
          <Stack direction="row" spacing={1} alignItems="center">
            <Typography variant="body2" color="text.secondary">
              Rows per page:
            </Typography>
            <Select
              size="small"
              value={rowsPerPage}
              onChange={(e) => setRowsPerPage(Number(e.target.value))}
              sx={{ height: 32, bgcolor: "white" }}
            >
              {[10, 25, 50, 100].map((n) => (
                <MenuItem key={n} value={n}>{n}</MenuItem>
              ))}
            </Select>
            <IconButton
              size="small"
              onClick={() => entListQuery.refetch()}
              disabled={entListQuery.isFetching}
              sx={{ ml: 1 }}
            >
              <RefreshIcon fontSize="small" />
            </IconButton>
          </Stack>

          <Stack direction="row" spacing={1} alignItems="center">
            <IconButton
              size="small"
              onClick={handlePreviousPage}
              disabled={tokensHistory.length === 0}
            >
              <ArrowBackIosNewIcon fontSize="small" />
            </IconButton>
            <Typography variant="body2" color="text.secondary">
              Page {tokensHistory.length + 1}
            </Typography>
            <IconButton
              size="small"
              onClick={handleNextPage}
              disabled={!entList?.hasNextPage}
            >
              <ArrowForwardIosIcon fontSize="small" />
            </IconButton>
          </Stack>

          <Box />
        </Box>
      </Paper>
    </Stack>
  );
}
