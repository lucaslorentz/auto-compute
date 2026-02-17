import {
  Box,
  Link,
  Typography,
} from "@mui/material";
import React from "react";
import { NavLink } from "react-router-dom";
import type { EntComputedModel, EntObserverModel } from "../models";

type ComputedLike = Pick<
  EntComputedModel | EntObserverModel,
  "allEntitiesDependencies" | "loadedEntitiesDependencies"
>;

export function ComputedDependencies({
  computed,
}: {
  computed: ComputedLike | null | undefined;
}) {
  if (!computed) return <>-</>;

  const allEntities = computed.allEntitiesDependencies ?? [];
  const loadedEntities = computed.loadedEntitiesDependencies ?? [];

  return (
    <Box>
      <DependencyList title={`All Entities (${allEntities.length})`} dependencies={allEntities} />
      <DependencyList title={`Loaded Entities (${loadedEntities.length})`} dependencies={loadedEntities} />
    </Box>
  );
}

function DependencyList(props: { title: string; dependencies: { entityName: string; memberName: string }[] }) {
  const { title, dependencies } = props;

  return (
    <details>
      <Typography
        component="summary"
        variant="caption"
        color="text.secondary"
        sx={{ fontWeight: 600, cursor: "pointer" }}
      >
        {title}
      </Typography>
      {dependencies.length === 0 ? (
        <Typography variant="caption" color="text.disabled" sx={{ display: "block", mb: 0.5 }}>
          None
        </Typography>
      ) : (
        <Box component="ul" sx={{ m: 0, pl: 2, maxHeight: 150, overflowY: "auto", mb: 0.5 }}>
          {dependencies.map((dep, i) => {
            return (
              <Typography
                key={`${dep.entityName}.${dep.memberName}.${i}`}
                component="li"
                variant="caption"
                color="text.secondary"
                sx={{
                  mb: 0.2,
                  lineBreak: "anywhere",
                  overflowWrap: "break-word",
                }}
              >
                <Link
                  component={NavLink}
                  to={`/${encodeURIComponent(dep.entityName)}/schema#${encodeURIComponent(dep.memberName)}`}
                   sx={{ color: 'primary.main', '&:hover': { textDecoration: 'underline' } }}
                >
                  {dep.entityName}.
                  <span style={{ fontWeight: 600 }}>{dep.memberName}</span>
                </Link>
              </Typography>
            );
          })}
        </Box>
      )}
    </details>
  );
}
