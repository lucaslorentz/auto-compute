import { Autocomplete, CircularProgress, TextField } from "@mui/material";
import React, { useEffect, useState } from "react";
import { useQuery } from "react-query";
import { apiFetch } from "../api";
import type { EntListModel } from "../models";

export function NavigationFilterAutocomplete({
  targetEntity,
  value,
  onChange,
}: {
  targetEntity: string;
  value: any;
  onChange: (value: any) => void;
}) {
  const [open, setOpen] = useState(false);
  const [inputValue, setInputValue] = useState("");

  const { data, isLoading } = useQuery(
    ["ents", targetEntity, "items", { search: inputValue }],
    () => apiFetch<EntListModel>(`/ents/${targetEntity}/items?search=${encodeURIComponent(inputValue)}&pageSize=20&include=Id,ToString()`),
    {
      enabled: open,
    }
  );

  const options = data?.entities ?? [];

  return (
    <Autocomplete
      size="small"
      open={open}
      onOpen={() => setOpen(true)}
      onClose={() => setOpen(false)}
      isOptionEqualToValue={(option, value) => String(option.id) === String(value)}
      getOptionLabel={(option) => String(option.propertyValues["ToString()"] || option.id)}
      options={options}
      loading={isLoading}
      value={options.find((o) => String(o.id) === String(value)) || null}
      onChange={(_event, newValue) => {
        onChange(newValue ? String(newValue.id) : null);
      }}
      onInputChange={(_event, newInputValue) => {
        setInputValue(newInputValue);
      }}
      renderInput={(params) => (
        <TextField
          {...params}
          label="Search..."
          InputProps={{
            ...params.InputProps,
            endAdornment: (
              <React.Fragment>
                {isLoading ? <CircularProgress color="inherit" size={20} /> : null}
                {params.InputProps.endAdornment}
              </React.Fragment>
            ),
          }}
        />
      )}
    />
  );
}
