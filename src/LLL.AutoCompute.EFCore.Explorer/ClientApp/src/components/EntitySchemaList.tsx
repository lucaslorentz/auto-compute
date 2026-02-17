import {
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
import React from "react";
import { useQuery } from "react-query";
import { NavLink } from "react-router-dom";
import { apiFetch } from "../api";
import type { EntModel } from "../models";

export function EntitySchemaList() {
  const entsQuery = useQuery(["ents"], {
    async queryFn() {
      return await apiFetch<EntModel[]>(`/ents`);
    },
  });

  const ents = entsQuery.data ?? [];

  if (entsQuery.isLoading) return <LinearProgress />;

  return (
    <Stack padding={3} spacing={3}>
      <Stack
        direction="row"
        justifyContent="space-between"
        alignItems="center"
      >
        <Typography variant="h4" fontWeight="bold">
          Entities
        </Typography>
        <Button
          component={NavLink}
          to="/computeds"
          variant="outlined"
          size="small"
        >
          Computeds
        </Button>
      </Stack>
      <TableContainer component={Paper}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Name</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {ents.map((ent) => (
              <TableRow key={ent.name} hover>
                <TableCell>
                  <Link
                    component={NavLink}
                    to={`${encodeURI(ent.name)}/schema`}
                    sx={{ textDecoration: "none", fontWeight: 500 }}
                  >
                    {ent.name}
                  </Link>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
    </Stack>
  );
}
