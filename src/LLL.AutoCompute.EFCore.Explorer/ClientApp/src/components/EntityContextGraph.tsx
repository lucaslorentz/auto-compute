import {
  Box,
  Dialog,
  DialogContent,
  DialogTitle,
  IconButton,
  Stack,
  Tooltip,
} from "@mui/material";
import { Close, AccountTree } from "@mui/icons-material";
import React, { useCallback, useState } from "react";
import type { FlowGraphModel } from "../models";
import { DependencyFlow } from "./DependencyFlow";

export function EntityContextGraph({
    computed,
}: {
    computed:
      | { entityContextGraph: FlowGraphModel }
      | null
      | undefined;
}) {
  const [open, setOpen] = useState(false);

  const handleOpen = useCallback(() => setOpen(true), []);
  const handleClose = useCallback(() => setOpen(false), []);

  if (!computed?.entityContextGraph) return null;

  return (
    <>
      <Tooltip title="View Graph">
        <IconButton size="small" onClick={handleOpen}>
          <AccountTree fontSize="small" />
        </IconButton>
      </Tooltip>
      <Dialog
        open={open}
        onClose={handleClose}
        maxWidth={false}
        fullWidth
        PaperProps={{
          sx: {
            width: "90vw",
            maxWidth: "90vw",
            height: "90vh",
            maxHeight: "90vh",
          },
        }}
      >
        <DialogTitle>
          <Stack
            direction="row"
            alignItems="center"
            justifyContent="space-between"
          >
            Dependency Graph
            <IconButton onClick={handleClose}>
              <Close />
            </IconButton>
          </Stack>
        </DialogTitle>
        <DialogContent sx={{ height: "100%", display: "flex" }}>
          <Box sx={{ width: "100%", height: "100%" }}>
            <DependencyFlow graph={computed.entityContextGraph} />
          </Box>
        </DialogContent>
      </Dialog>
    </>
  );
}
