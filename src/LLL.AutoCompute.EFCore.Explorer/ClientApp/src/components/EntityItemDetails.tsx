import AutoFixNormalIcon from "@mui/icons-material/AutoFixNormal";
import CheckCircleIcon from "@mui/icons-material/CheckCircle";
import InfoOutlinedIcon from "@mui/icons-material/InfoOutlined";
import WarningIcon from "@mui/icons-material/Warning";
import {
  Breadcrumbs,
  Button,
  Chip,
  LinearProgress,
  Link,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Tooltip,
  Typography,
} from "@mui/material";
import React from "react";
import { useMutation, useQuery, useQueryClient } from "react-query";
import { NavLink, useParams } from "react-router-dom";
import { apiFetch, apiPost } from "../api";
import type { EntDataModel, EntDetailsModel } from "../models";
import { NavigationRenderer } from "./NavigationRenderer";
import { ValueRenderer } from "./ValueRenderer";

export function EntityItemDetails() {
  const { name, id } = useParams();

  const entSchemaQuery = useQuery(["ents", name], () => apiFetch<EntDetailsModel>(`/ents/${name}`), { enabled: !!name });
  const entDataQuery = useQuery(["ents", name, "items", id], () => apiFetch<EntDataModel>(`/ents/${name}/items/${id}`), { enabled: !!name && !!id });

  const queryClient = useQueryClient();
  const fixMutation = useMutation({
    mutationFn: async ({ memberName }: { memberName?: string }) => {
      await apiPost(`/ents/${name}/items/${id}/fix${memberName ? `?memberName=${memberName}` : ""}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries(["ents", name, "items", id]);
    },
  });

  const entSchema = entSchemaQuery.data;
  const entData = entDataQuery.data;

  if (!entSchema || !entData) return <LinearProgress />;

  const hasInconsistencies = Object.values(entData.membersConsistency).some((v) => v !== true);

  return (
    <Stack padding={3} spacing={3}>
      <Stack spacing={1}>
        <Breadcrumbs aria-label="breadcrumb">
          <Link component={NavLink} to="/" color="inherit">
            Entities
          </Link>
          <Link component={NavLink} to={`/${encodeURIComponent(name!)}/schema`} color="inherit">
            {entSchema.name}
          </Link>
          <Link component={NavLink} to={`/${encodeURIComponent(name!)}/items`} color="inherit">
            Items
          </Link>
          <Typography color="text.primary">ID: {String(entData.id)}</Typography>
        </Breadcrumbs>
        <Stack direction="row" justifyContent="space-between" alignItems="center">
          <Typography variant="h4">
            Item Detail
          </Typography>
          {hasInconsistencies && (
            <Button
              color="error"
              startIcon={<AutoFixNormalIcon />}
              onClick={() => fixMutation.mutate({})}
              disabled={fixMutation.isLoading}
            >
              Fix All Inconsistencies
            </Button>
          )}
        </Stack>
      </Stack>

      <details open>
        <Typography component="summary" variant="h5" gutterBottom sx={{ mb: 2 }}>
          Properties
        </Typography>
        <TableContainer component={Paper} variant="outlined">
          <Table sx={{ tableLayout: "fixed", lineBreak: "anywhere" }}>
            <TableHead>
              <TableRow>
                <TableCell width="30%">Member</TableCell>
                <TableCell>Persisted Value</TableCell>
                <TableCell>Type / Meta</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {entSchema.properties.map((property) => {
                const value = entData.propertyValues[property.name];
                const computedValue = entData.computedValues[property.name];
                const isConsistent = entData.membersConsistency[property.name] === true;

                return (
                  <TableRow key={property.name}>
                    <TableCell>
                      <Stack direction="row" spacing={1} alignItems="center">
                        <Typography variant="body2" fontWeight={500}>
                          {property.name}
                        </Typography>
                        {property.isPrimaryKey && (
                          <Chip size="small" label="PK" color="primary" variant="outlined" />
                        )}
                      </Stack>
                    </TableCell>
                    <TableCell>
                      <Stack direction="row" spacing={1} alignItems="center">
                        <ValueRenderer value={value} property={property} />
                        {property.computed && (
                          isConsistent ? (
                            <Tooltip title="Consistent">
                              <CheckCircleIcon color="success" sx={{ fontSize: 16 }} />
                            </Tooltip>
                          ) : (
                            <Stack direction="row" spacing={1} alignItems="center">
                              <Tooltip title={`Inconsistent! Computed value is: ${JSON.stringify(computedValue)}`}>
                                <WarningIcon color="warning" fontSize="small" />
                              </Tooltip>
                              <Button
                                size="small"
                                variant="outlined"
                                color="error"
                                startIcon={<AutoFixNormalIcon sx={{ fontSize: 16 }} />}
                                onClick={() => fixMutation.mutate({ memberName: property.name })}
                                disabled={fixMutation.isLoading}
                              >
                                Fix
                              </Button>
                            </Stack>
                          )
                        )}
                      </Stack>
                    </TableCell>
                    <TableCell>
                      <Stack direction="row" spacing={1} alignItems="center">
                        <Typography variant="caption" color="text.secondary">
                          {property.clrType}
                        </Typography>
                        {property.computed && (
                          <Chip icon={<InfoOutlinedIcon style={{ fontSize: 14 }} />} label="Computed" size="small" color="info" variant="outlined" />
                        )}
                      </Stack>
                    </TableCell>
                  </TableRow>
                );
              })}
            </TableBody>
          </Table>
        </TableContainer>
      </details>

      {entSchema.navigations.length > 0 && (
        <details open>
          <Typography component="summary" variant="h5">
            Navigations
          </Typography>
          <TableContainer component={Paper}>
            <Table sx={{ tableLayout: "fixed", lineBreak: "anywhere" }}>
              <TableHead>
                <TableRow>
                  <TableCell width="30%">Navigation</TableCell>
                  <TableCell>Linked Item</TableCell>
                  <TableCell>Target Entity</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {entSchema.navigations.map((nav) => (
                  <TableRow key={nav.name}>
                    <TableCell>
                      <Stack direction="row" spacing={1} alignItems="center">
                        <Typography variant="body2" fontWeight={500}>{nav.name}</Typography>
                        {nav.isCollection && <Chip size="small" label="Collection" variant="outlined" />}
                      </Stack>
                    </TableCell>
                    <TableCell>
                      <NavigationRenderer
                        navigation={nav}
                        value={entData.referenceValues[nav.name]}
                        sourceId={entData.id}
                      />
                    </TableCell>
                    <TableCell>
                      <Link component={NavLink} to={`/${encodeURIComponent(nav.targetEntity)}/schema`} variant="caption">
                        {nav.targetEntity}
                      </Link>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </details>
      )}
    </Stack>
  );
}
