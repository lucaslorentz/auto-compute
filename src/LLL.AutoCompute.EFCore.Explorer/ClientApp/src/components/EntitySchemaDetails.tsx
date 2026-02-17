import {
  Breadcrumbs,
  Chip,
  Link,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
  Button,
  LinearProgress,
} from "@mui/material";
import ListIcon from "@mui/icons-material/List";
import React from "react";
import { useQuery } from "react-query";
import { NavLink, useParams } from "react-router-dom";
import { apiFetch } from "../api";
import type { EntDetailsModel } from "../models";
import { Consistency } from "./Consistency";
import { ComputedDependencies } from "./ComputedDependencies";
import { ComputedExpression } from "./ComputedExpression";
import { EntityContextGraph } from "./EntityContextGraph";
import { useScrollToAnchor } from "./useScrollToAnchor";

export function EntitySchemaDetails() {
  const { name } = useParams();
  useScrollToAnchor();

  const entSchemaQuery = useQuery(["ents", name], {
    async queryFn() {
      return await apiFetch<EntDetailsModel>(`/ents/${name}`);
    },
    enabled: !!name,
  });

  const entSchema = entSchemaQuery.data;

  if (!entSchema) return <LinearProgress />;

  return (
    <Stack padding={3} spacing={3}>
      <Stack spacing={1}>
        <Breadcrumbs aria-label="breadcrumb">
          <Link component={NavLink} to="/" color="inherit">
            Entities
          </Link>
        </Breadcrumbs>
        <Stack direction="row" justifyContent="space-between" alignItems="center">
          <Typography variant="h4">
            {entSchema.name}
          </Typography>
          <Button
            component={NavLink}
            to={`/${encodeURIComponent(name!)}/items`}
            startIcon={<ListIcon />}
          >
            View Items
          </Button>
        </Stack>
      </Stack>

      <details open>
        <Typography component="summary" variant="h5" gutterBottom sx={{ cursor: 'pointer', mb: 2 }}>
          Properties
        </Typography>
        <TableContainer component={Paper}>
          <Table sx={{ lineBreak: "anywhere" }}>
            <TableHead>
              <TableRow>
                <TableCell width="20%">Name</TableCell>
                <TableCell width="25%">Type</TableCell>
                <TableCell width={350}>Consistency (Since / Status)</TableCell>
                <TableCell>Dependencies</TableCell>
                <TableCell width="1%" align="center" sx={{ whiteSpace: "nowrap" }}>Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {entSchema.properties.map((property) => (
                <TableRow key={property.name} id={property.name}>
                  <TableCell>
                    <Stack direction="row" spacing={0.5} alignItems="center" flexWrap="wrap">
                      <Typography variant="body2" fontWeight={500}>
                        {property.name}
                      </Typography>
                      {property.isPrimaryKey && (
                        <Chip size="small" label="PK" color="info" variant="outlined" />
                      )}
                      {property.isShadow && (
                        <Chip size="small" label="Shadow" variant="outlined" />
                      )}
                    </Stack>
                  </TableCell>
                  <TableCell>
                    <Typography variant="caption" color="text.secondary">
                      {property.clrType}
                    </Typography>
                  </TableCell>
                  <TableCell sx={{ p: 1 }}>
                    {property.computed ? (
                      <Consistency entityName={entSchema.name} memberName={property.name} />
                    ) : (
                      "-"
                    )}
                  </TableCell>
                  <TableCell>
                    {property.computed ? (
                      <ComputedDependencies computed={property.computed} />
                    ) : (
                      "-"
                    )}
                  </TableCell>
                  <TableCell align="center">
                    {property.computed ? (
                      <Stack direction="row" spacing={0.5} justifyContent="center">
                        <ComputedExpression computed={property.computed} />
                        <EntityContextGraph computed={property.computed} />
                      </Stack>
                    ) : (
                      "-"
                    )}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      </details>

      {entSchema.navigations.length > 0 && (
        <details open>
          <Typography component="summary" variant="h5" gutterBottom sx={{ cursor: 'pointer', mb: 2 }}>
            Navigations
          </Typography>
          <TableContainer component={Paper}>
            <Table sx={{ lineBreak: "anywhere" }}>
              <TableHead>
                <TableRow>
                  <TableCell width="20%">Name</TableCell>
                  <TableCell width="25%">Target</TableCell>
                  <TableCell width={350}>Consistency</TableCell>
                  <TableCell>Dependencies</TableCell>
                  <TableCell width="1%" align="center" sx={{ whiteSpace: "nowrap" }}>Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {entSchema.navigations.map((nav) => (
                  <TableRow key={nav.name} id={nav.name}>
                    <TableCell>
                      <Stack direction="row" spacing={0.5} alignItems="center" flexWrap="wrap">
                        <Typography variant="body2" fontWeight={500}>
                          {nav.name}
                        </Typography>
                        {nav.isCollection && (
                          <Chip size="small" label="Collection" color="info" variant="outlined" />
                        )}
                      </Stack>
                    </TableCell>
                    <TableCell>
                      <Link component={NavLink} to={`/${encodeURIComponent(nav.targetEntity)}/schema`} variant="caption">
                        {nav.targetEntity}
                      </Link>
                    </TableCell>
                    <TableCell sx={{ p: 1 }}>
                      {nav.computed ? (
                        <Consistency entityName={entSchema.name} memberName={nav.name} />
                      ) : (
                        "-"
                      )}
                    </TableCell>
                    <TableCell>
                      {nav.computed ? (
                        <ComputedDependencies computed={nav.computed} />
                      ) : (
                        "-"
                      )}
                    </TableCell>
                    <TableCell align="center">
                      {nav.computed ? (
                        <Stack direction="row" spacing={0.5} justifyContent="center">
                          <ComputedExpression computed={nav.computed} />
                          <EntityContextGraph computed={nav.computed} />
                        </Stack>
                      ) : (
                        "-"
                      )}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </details>
      )}

      {entSchema.methods.length > 0 && (
        <details open>
          <Typography component="summary" variant="h5" gutterBottom sx={{ cursor: 'pointer', mb: 2 }}>
            Methods
          </Typography>
          <TableContainer component={Paper}>
            <Table sx={{ lineBreak: "anywhere" }}>
              <TableHead>
                <TableRow>
                  <TableCell width="20%">Name</TableCell>
                  <TableCell>Return Type</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {entSchema.methods.map((method) => (
                  <TableRow key={method.name}>
                    <TableCell>
                      <Typography variant="body2" fontWeight={500}>
                        {method.name}
                      </Typography>
                    </TableCell>
                    <TableCell>
                      <Typography variant="caption" color="text.secondary">
                        {method.clrType}
                      </Typography>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </details>
      )}

      {entSchema.observers.length > 0 && (
        <details open>
          <Typography component="summary" variant="h5" gutterBottom sx={{ cursor: 'pointer', mb: 2 }}>
            Observers
          </Typography>
          <TableContainer component={Paper}>
            <Table sx={{ lineBreak: "anywhere" }}>
              <TableHead>
                <TableRow>
                  <TableCell width="20%">Name</TableCell>
                  <TableCell>Dependencies</TableCell>
                  <TableCell width="1%" align="center" sx={{ whiteSpace: "nowrap" }}>Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {entSchema.observers.map((observer, i) => (
                  <TableRow key={i} id={observer.name}>
                    <TableCell sx={{ fontWeight: 500 }}>
                      {observer.name}
                    </TableCell>
                    <TableCell>
                      <ComputedDependencies computed={observer} />
                    </TableCell>
                    <TableCell align="center">
                      <Stack direction="row" spacing={0.5} justifyContent="center">
                        <ComputedExpression computed={observer} />
                        <EntityContextGraph computed={observer} />
                      </Stack>
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
