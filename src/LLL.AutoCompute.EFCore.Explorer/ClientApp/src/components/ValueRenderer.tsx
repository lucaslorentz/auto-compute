import { Typography } from "@mui/material";
import React from "react";
import type { EntEnumItemModel } from "../models";

export function ValueRenderer({
  value,
  property,
}: {
  value: any;
  property: {
    clrType: string;
    enumItems?: Record<string, EntEnumItemModel | undefined> | null;
  };
}) {
  if (value === null || value === undefined) {
    return (
      <Typography
        variant="caption"
        sx={{ fontStyle: "italic", color: "text.disabled" }}
      >
        null
      </Typography>
    );
  }

  const type = property.clrType.toLowerCase();

  if (property.enumItems) {
    const item = property.enumItems[String(value)];
    return (
      <Typography variant="body2">
        {item ? item.label : String(value)}
      </Typography>
    );
  }

  if (type.includes("boolean")) {
    return (
      <Typography variant="body2">{String(value).toLowerCase()}</Typography>
    );
  }

  if (type.includes("datetime") || type.includes("dateonly")) {
    const date = new Date(value);
    if (!isNaN(date.getTime())) {
      const iso = date.toJSON();
      const text = type.includes("dateonly")
        ? iso.substring(0, 10)
        : iso.replace("T", " ").substring(0, 19);

      return <Typography variant="body2">{text}</Typography>;
    }
  }

  if (typeof value === "object") {
    return <Typography variant="body2">{JSON.stringify(value)}</Typography>;
  }

  return <Typography variant="body2">{String(value)}</Typography>;
}
