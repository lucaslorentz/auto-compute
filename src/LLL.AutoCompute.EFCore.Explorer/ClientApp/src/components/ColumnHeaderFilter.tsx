import { MoreVert } from "@mui/icons-material";
import ArrowDownwardIcon from "@mui/icons-material/ArrowDownward";
import ArrowUpwardIcon from "@mui/icons-material/ArrowUpward";
import ClearIcon from "@mui/icons-material/Clear";
import {
  Box,
  Chip,
  Divider,
  FormControlLabel,
  IconButton,
  ListItemIcon,
  ListItemText,
  Menu,
  MenuItem,
  Stack,
  Switch,
  Tooltip,
  Typography,
} from "@mui/material";
import { DatePicker } from "@mui/x-date-pickers/DatePicker";
import React, { useState } from "react";

export function ColumnHeaderFilter({
  label,
  property,
  active,
  activeValue,
  onClear,
  sortBy,
  sortDescending,
  onSort,
  isComputed,
  isInconsistent,
  onToggleInconsistent,
  since,
  onSinceChange,
  hideButton,
  onFilterMenuOpen,
  onFilterMenuClose,
  children,
}: {
  label: string;
  property?: string;
  active?: boolean;
  activeValue?: any;
  onClear?: () => void;
  sortBy?: string | null;
  sortDescending?: boolean;
  onSort?: (property: string | null, descending: boolean) => void;
  isComputed?: boolean;
  isInconsistent?: boolean;
  onToggleInconsistent?: (val: boolean) => void;
  since?: Date | null;
  onSinceChange?: (date: Date | null) => void;
  hideButton?: boolean;
  onFilterMenuOpen?: () => void;
  onFilterMenuClose?: () => void;
  children?: React.ReactNode;
}) {
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const [datePickerOpen, setDatePickerOpen] = useState(false);
  const open = Boolean(anchorEl);

  const handleClick = (event: React.MouseEvent<HTMLElement>) => {
    setAnchorEl(event.currentTarget);
    onFilterMenuOpen?.();
  };

  const handleClose = () => {
    if (datePickerOpen) return; // let the DatePicker close first
    setAnchorEl(null);
    onFilterMenuClose?.();
  };

  const isSorted = property && sortBy === property;

  const handleTitleClick = () => {
    if (!property || !onSort) return;
    if (!isSorted) onSort(property, false);           // unsorted → asc
    else if (!sortDescending) onSort(property, true); // asc → desc
    else onSort(null, false);                          // desc → clear
  };

  return (
    <Stack spacing={0.25}>
      <Stack direction="row" alignItems="center" justifyContent="space-between">
        <Stack
          direction="row"
          alignItems="center"
          spacing={0.5}
          onClick={onSort ? handleTitleClick : undefined}
          sx={onSort ? { cursor: "pointer", userSelect: "none", "&:hover": { color: "primary.main" } } : undefined}
        >
          <Typography variant="body2" fontWeight="bold">
            {label}
          </Typography>
          {isSorted && (
            <Box sx={{ color: "primary.main", display: "flex", alignItems: "center" }}>
              {sortDescending ? (
                <ArrowDownwardIcon fontSize="inherit" />
              ) : (
                <ArrowUpwardIcon fontSize="inherit" />
              )}
            </Box>
          )}
        </Stack>
        {!hideButton && (
          <IconButton size="small" onClick={handleClick} sx={{ p: 0.25 }}>
            <MoreVert fontSize="inherit" />
          </IconButton>
        )}
      </Stack>

      {active && activeValue !== undefined && (
        <Box sx={{ display: "flex" }}>
          <Tooltip title={`Filter: ${activeValue}`}>
            <Chip
              size="small"
              color="primary"
              label={activeValue}
              onDelete={onClear}
              deleteIcon={<ClearIcon />}
              sx={{
                height: 20,
                maxWidth: "100%",
                "& .MuiChip-label": { px: 1, fontSize: "0.7rem" },
              }}
            />
          </Tooltip>
        </Box>
      )}

      {!hideButton && (
        <Menu
          anchorEl={anchorEl}
          open={open}
          onClose={handleClose}
          PaperProps={{ sx: { minWidth: 280 } }}
        >
          {property && onSort && (
            <>
              <MenuItem
                onClick={() => {
                  onSort(property, false);
                  handleClose();
                }}
              >
                <ListItemIcon>
                  <ArrowUpwardIcon fontSize="small" />
                </ListItemIcon>
                <ListItemText>Sort Ascending</ListItemText>
              </MenuItem>
              <MenuItem
                onClick={() => {
                  onSort(property, true);
                  handleClose();
                }}
              >
                <ListItemIcon>
                  <ArrowDownwardIcon fontSize="small" />
                </ListItemIcon>
                <ListItemText>Sort Descending</ListItemText>
              </MenuItem>
            </>
          )}

          {isComputed && (
            <>
              <Divider />
              <MenuItem>
                <FormControlLabel
                  control={
                    <Switch
                      size="small"
                      checked={isInconsistent || false}
                      onChange={(e) => onToggleInconsistent?.(e.target.checked)}
                    />
                  }
                  label="Only Inconsistent"
                />
              </MenuItem>
              {isInconsistent && (
                <Box sx={{ px: 2, py: 1 }}>
                  <DatePicker
                    label="Since"
                    value={since ?? null}
                    onChange={(val) => onSinceChange?.(val)}
                    open={datePickerOpen}
                    onOpen={() => setDatePickerOpen(true)}
                    onClose={() => setDatePickerOpen(false)}
                    slotProps={{ textField: { size: "small", fullWidth: true } }}
                  />
                </Box>
              )}
            </>
          )}

          <Divider />
          <Box sx={{ p: 2 }}>{children}</Box>
        </Menu>
      )}
    </Stack>
  );
}
