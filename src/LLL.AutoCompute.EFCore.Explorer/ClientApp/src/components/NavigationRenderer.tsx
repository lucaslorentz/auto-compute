import { Link, Typography } from "@mui/material";
import React from "react";
import { NavLink } from "react-router-dom";
import type { EntityReferenceModel } from "../models";
import { isNullOrUndefined } from "../utils";

type Props = {
  navigation: {
    targetEntity: string;
    filterKey?: string | null;
    isCollection?: boolean;
  };
  value: EntityReferenceModel | null | undefined;
  sourceId: {} | null | undefined;
};

export function NavigationRenderer({ navigation, value, sourceId }: Props) {
  if (navigation.isCollection) {
    const to = {
      pathname: `/${encodeURIComponent(navigation.targetEntity)}/items`,
      search: `?columnFilters=${encodeURIComponent(
        JSON.stringify({ [String(navigation.filterKey)]: sourceId })
      )}&include=Id,ToString(),${encodeURIComponent(String(navigation.filterKey))}`,
    };

    return (
      <Link component={NavLink} to={to}>
        {value?.count ?? 0} items
      </Link>
    );
  } else if (!isNullOrUndefined(value)) {
    const to = `/${encodeURIComponent(
      navigation.targetEntity
    )}/items/${encodeURIComponent(String(value.id))}`;

    return (
      <Link component={NavLink} to={to}>
        {value.toStringValue ?? String(value.id)}
      </Link>
    );
  } else {
    return (
      <Typography
        variant="body2"
        color="text.disabled"
        sx={{ fontStyle: "italic" }}
      >
        null
      </Typography>
    );
  }
}

export default NavigationRenderer;
