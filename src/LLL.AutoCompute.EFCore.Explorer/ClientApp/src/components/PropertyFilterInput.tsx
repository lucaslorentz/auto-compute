import {
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
} from "@mui/material";
import { DatePicker } from "@mui/x-date-pickers";
import { DateTimePicker } from "@mui/x-date-pickers/DateTimePicker";
import React from "react";
import type { EntPropertyModel, EntEnumItemModel } from "../models";

export function PropertyFilterInput({
  property,
  values,
  onChange,
}: {
  property: EntPropertyModel & {
    enumItems?: Partial<Record<string, EntEnumItemModel>> | null;
  };
  values: Record<string, unknown>;
  onChange: (key: string, val: unknown) => void;
}) {
  const type = property.clrType.toLowerCase();

  if (type === "system.boolean" || type === "nullable<system.boolean>") {
    return (
      <FormControl size="small" fullWidth>
        <InputLabel id={`filter-${property.name}-label`}>Status</InputLabel>
        <Select
          labelId={`filter-${property.name}-label`}
          label="Status"
          value={values[property.name] || ""}
          onChange={(e) => onChange(property.name, e.target.value as string)}
        >
          <MenuItem value="">
            <em>Any</em>
          </MenuItem>
          <MenuItem value="true">True</MenuItem>
          <MenuItem value="false">False</MenuItem>
        </Select>
      </FormControl>
    );
  }

  if (property.enumItems) {
    return (
      <FormControl size="small" fullWidth>
        <InputLabel id={`filter-${property.name}-label`}>Value</InputLabel>
        <Select
          labelId={`filter-${property.name}-label`}
          label="Value"
          value={values[property.name] || ""}
          onChange={(e) => onChange(property.name, e.target.value as string)}
        >
          <MenuItem value="">
            <em>Any</em>
          </MenuItem>
          {Object.values(property.enumItems)
            .filter((val): val is EntEnumItemModel => val !== undefined)
            .map((val) => (
              <MenuItem key={val.value} value={val.value}>
                {val.label}
              </MenuItem>
            ))}
        </Select>
      </FormControl>
    );
  }

  const isNumeric = [
    "system.int32",
    "system.int64",
    "system.decimal",
    "system.double",
    "system.single",
    "system.byte",
    "system.short",
  ].some((t) => type.includes(t));

  if (isNumeric) {
    return (
      <Stack spacing={2} sx={{ pt: 1 }}>
        <TextField
          size="small"
          type="number"
          fullWidth
          label="Equal"
          value={values[property.name] || ""}
          onChange={(e) => onChange(property.name, e.target.value)}
          InputLabelProps={{ shrink: true }}
        />
        <TextField
          size="small"
          type="number"
          fullWidth
          label="Minimum"
          value={values[`${property.name}_gte`] || ""}
          onChange={(e) => onChange(`${property.name}_gte`, e.target.value)}
          InputLabelProps={{ shrink: true }}
        />
        <TextField
          size="small"
          type="number"
          fullWidth
          label="Maximum"
          value={values[`${property.name}_lte`] || ""}
          onChange={(e) => onChange(`${property.name}_lte`, e.target.value)}
          InputLabelProps={{ shrink: true }}
        />
      </Stack>
    );
  }

  if (type.includes("dateonly")) {
    const toDateStr = (d: Date | null): string => {
      if (!d || isNaN(d.getTime())) return "";
      const y = d.getFullYear();
      const m = String(d.getMonth() + 1).padStart(2, "0");
      const day = String(d.getDate()).padStart(2, "0");
      return `${y}-${m}-${day}`;
    };
    const parseDate = (v: unknown): Date | null => {
      if (typeof v !== "string" || !v) return null;
      const s = v.length > 10 ? v.substring(0, 10) : v;
      const d = new Date(`${s}T00:00:00`);
      return isNaN(d.getTime()) ? null : d;
    };
    const slotProps = {
      textField: { size: "small" as const, fullWidth: true, InputLabelProps: { shrink: true } },
    };
    return (
      <Stack spacing={2} sx={{ pt: 1 }}>
        <DatePicker label="Equal Date" value={parseDate(values[property.name])} onChange={(val) => onChange(property.name, toDateStr(val))} slotProps={slotProps} />
        <DatePicker label="Start Date" value={parseDate(values[`${property.name}_gte`])} onChange={(val) => onChange(`${property.name}_gte`, toDateStr(val))} slotProps={slotProps} />
        <DatePicker label="End Date" value={parseDate(values[`${property.name}_lte`])} onChange={(val) => onChange(`${property.name}_lte`, toDateStr(val))} slotProps={slotProps} />
      </Stack>
    );
  }

  if (type.includes("system.datetime")) {
    const toDateTimeStr = (d: Date | null): string => {
      if (!d || isNaN(d.getTime())) return "";
      const pad = (n: number) => String(n).padStart(2, "0");
      return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`;
    };
    const parseDateTime = (v: unknown): Date | null => {
      if (typeof v !== "string" || !v) return null;
      const d = new Date(v.includes("T") ? v : `${v}T00:00:00`);
      return isNaN(d.getTime()) ? null : d;
    };
    const slotProps = {
      textField: { size: "small" as const, fullWidth: true, InputLabelProps: { shrink: true } },
    };
    return (
      <Stack spacing={2} sx={{ pt: 1 }}>
        <DateTimePicker label="Start DateTime" value={parseDateTime(values[`${property.name}_gte`])} onChange={(val) => onChange(`${property.name}_gte`, toDateTimeStr(val))} slotProps={slotProps} />
        <DateTimePicker label="End DateTime" value={parseDateTime(values[`${property.name}_lte`])} onChange={(val) => onChange(`${property.name}_lte`, toDateTimeStr(val))} slotProps={slotProps} />
      </Stack>
    );
  }

  return (
    <TextField
      size="small"
      fullWidth
      label="Filter"
      value={values[property.name] || ""}
      onChange={(e) => onChange(property.name, e.target.value)}
      InputLabelProps={{ shrink: true }}
    />
  );
}
