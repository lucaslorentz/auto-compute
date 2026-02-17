import {
  Button,
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
  LinearProgress,
} from "@mui/material";
import React from "react";
import { useQuery } from "react-query";
import { NavLink } from "react-router-dom";
import { apiFetch } from "../api";
import type { EntComputedModel, EntObserverModel } from "../models";
import { Consistency } from "./Consistency";
import { ComputedDependencies } from "./ComputedDependencies";
import { ComputedExpression } from "./ComputedExpression";
import { EntityContextGraph } from "./EntityContextGraph";

export function ComputedMembersList() {
  const computedsQuery = useQuery(["computeds"], () => apiFetch<EntComputedModel[]>(`/computeds`));
  const observersQuery = useQuery(["observers"], () => apiFetch<EntObserverModel[]>(`/observers`));

  const computeds = computedsQuery.data;
  const observers = observersQuery.data;

  if (!computeds || !observers) return <LinearProgress />;

  return (
    <Stack padding={3} spacing={3}>
      <Stack direction="row" justifyContent="space-between" alignItems="center">
        <Stack spacing={0.5}>
          <Typography variant="h4" fontWeight="bold">
            Computeds
          </Typography>
        </Stack>
        <Button
          variant="outlined"
          component={NavLink}
          to="/"
        >
          View Entities
        </Button>
      </Stack>

      <details open>
        <Typography component="summary" variant="h5" gutterBottom sx={{ cursor: 'pointer', mb: 2 }}>
          Members
        </Typography>
        <TableContainer component={Paper} variant="outlined">
          <Table sx={{ lineBreak: "anywhere" }}>
            <TableHead sx={{ bgcolor: 'action.hover' }}>
              <TableRow>
                <TableCell width="20%" sx={{ fontWeight: 'bold' }}>Entity</TableCell>
                <TableCell width="15%" sx={{ fontWeight: 'bold' }}>Name</TableCell>
                <TableCell width={350} sx={{ fontWeight: 'bold' }}>Consistency (Since / Status)</TableCell>
                <TableCell sx={{ fontWeight: 'bold' }}>Dependencies</TableCell>
                <TableCell width="1%" align="center" sx={{ fontWeight: 'bold', whiteSpace: "nowrap" }}>Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {computeds.map((c, i) => (
                <TableRow key={i} hover>
                  <TableCell sx={{ fontWeight: 500, wordBreak: 'break-all' }}>
                      <Link component={NavLink} to={`/${encodeURIComponent(c.entity)}/schema`}>
                        {c.entity}
                      </Link>
                  </TableCell>
                  <TableCell sx={{ fontWeight: 500 }}>
                    <Link component={NavLink} to={`/${encodeURIComponent(c.entity)}/schema#${encodeURIComponent(c.member)}`}>
                      {c.member}
                    </Link>
                  </TableCell>
                  <TableCell sx={{ p: 1 }}>
                    <Consistency entityName={c.entity} memberName={c.member} />
                  </TableCell>
                  <TableCell sx={{ verticalAlign: 'middle' }}>
                    <ComputedDependencies computed={c} />
                  </TableCell>
                  <TableCell align="center">
                    <Stack direction="row" spacing={0.5} justifyContent="center">
                      <ComputedExpression computed={c} />
                      <EntityContextGraph computed={c} />
                    </Stack>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      </details>

      <details open>
        <Typography component="summary" variant="h5" gutterBottom sx={{ cursor: 'pointer', mb: 2 }}>
          Observers
        </Typography>
        <TableContainer component={Paper} variant="outlined">
          <Table sx={{ lineBreak: "anywhere" }}>
            <TableHead sx={{ bgcolor: 'action.hover' }}>
              <TableRow>
                <TableCell width="20%" sx={{ fontWeight: 'bold' }}>Entity</TableCell>
                <TableCell width="15%" sx={{ fontWeight: 'bold' }}>Name</TableCell>
                <TableCell sx={{ fontWeight: 'bold' }}>Dependencies</TableCell>
                <TableCell width="1%" align="center" sx={{ fontWeight: 'bold', whiteSpace: "nowrap" }}>Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {observers.map((o, i) => (
                <TableRow key={i} hover>
                  <TableCell sx={{ fontWeight: 500, wordBreak: 'break-all' }}>
                      <Link component={NavLink} to={`/${encodeURIComponent(o.entity)}/schema`}>
                        {o.entity}
                      </Link>
                  </TableCell>
                  <TableCell sx={{ fontWeight: 500 }}>
                    <Link component={NavLink} to={`/${encodeURIComponent(o.entity)}/schema#${encodeURIComponent(o.name)}`}>
                      {o.name}
                    </Link>
                  </TableCell>
                  <TableCell sx={{ verticalAlign: 'middle' }}>
                    <ComputedDependencies computed={o} />
                  </TableCell>
                  <TableCell align="center">
                    <Stack direction="row" spacing={0.5} justifyContent="center">
                      <ComputedExpression computed={o} />
                      <EntityContextGraph computed={o} />
                    </Stack>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      </details>
    </Stack>
  );
}
