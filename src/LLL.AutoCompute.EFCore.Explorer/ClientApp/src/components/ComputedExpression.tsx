import { Close, Code } from "@mui/icons-material";
import {
  Box,
  Dialog,
  DialogContent,
  DialogTitle,
  IconButton,
  Stack,
  Tooltip,
} from "@mui/material";
import React, { useCallback } from "react";

export function ComputedExpression(props: {
  computed: { expression?: string } | null | undefined;
}) {
  const { computed } = props;
  const [open, setOpen] = React.useState(false);

  if (!computed?.expression) return null;

  const handleOpen = useCallback(() => setOpen(true), []);
  const handleClose = useCallback(() => setOpen(false), []);

  return (
    <>
      <Tooltip title="View Expression">
        <IconButton size="small" onClick={handleOpen}>
          <Code fontSize="small" />
        </IconButton>
      </Tooltip>
      <Dialog open={open} onClose={handleClose} maxWidth="md" fullWidth>
        <DialogTitle>
          <Stack
            direction="row"
            alignItems="center"
            justifyContent="space-between"
          >
            Computed Expression
            <IconButton onClick={handleClose}>
              <Close />
            </IconButton>
          </Stack>
        </DialogTitle>
        <DialogContent dividers>
          <Box
            sx={{
              fontFamily: "monospace",
              bgcolor: 'rgba(0,0,0,0.03)',
              p: 2,
              borderRadius: 1,
              whiteSpace: 'pre-wrap',
              wordBreak: 'break-all'
            }}
          >
            {computed.expression}
          </Box>
        </DialogContent>
      </Dialog>
    </>
  );
}
