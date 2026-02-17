import { DatePicker } from "@mui/x-date-pickers";
import { Stack, Typography, CircularProgress, IconButton, Link, Box, Tooltip } from "@mui/material";
import AutoFixNormalIcon from "@mui/icons-material/AutoFixNormal";
import { CheckCircle, Warning } from "@mui/icons-material";
import React, { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "react-query";
import { useNavigate } from "react-router-dom";
import { apiFetch, apiPost } from "../api";
import type { ConsistencyModel } from "../models";
import { formatNumber } from "../utils";

export function Consistency({
  entityName,
  memberName,
}: {
  entityName: string;
  memberName: string;
}) {
  const oneMonthAgo = new Date();
  oneMonthAgo.setMonth(oneMonthAgo.getMonth() - 1);
  const [since, setSince] = useState<Date | null>(oneMonthAgo);
  const queryClient = useQueryClient();
  const navigate = useNavigate();

  const consistencyQuery = useQuery(
    ["ents", entityName, "members", memberName, "consistency", since?.toISOString()],
    () => apiFetch<ConsistencyModel>(`/ents/${entityName}/members/${memberName}/consistency?since=${since?.toISOString() || ""}`),
    { enabled: !!entityName && !!memberName, retry: false }
  );

  const fixMutation = useMutation(
    () => apiPost(`/ents/${entityName}/members/${memberName}/consistency/fix?since=${since?.toISOString() || ""}`),
    {
      onSuccess: () => {
        queryClient.invalidateQueries(["ents", entityName, "members", memberName, "consistency"]);
      },
    }
  );

  const consistency = consistencyQuery.data;
  const isHealthy = consistency && consistency.consistencyPercentage === 100;

  const handleNavigateToInconsistent = () => {
    const parts = [
      `include=Id,ToString(),${memberName}`,
      `inconsistentMember=${memberName}`,
    ];
    if (since) parts.push(`since=${since.toISOString().slice(0, 10)}`);
    navigate({ pathname: `/${entityName}/items`, search: `?${parts.join("&")}` });
  };

  return (
    <Stack direction="row" spacing={1} alignItems="center">
      <Box sx={{ width: 140 }}>
        <DatePicker
          value={since}
          onChange={(val) => setSince(val)}
          slotProps={{
            textField: {
              size: "small",
              sx: { width: "100%" },
            },
          }}
        />
      </Box>

      <Box sx={{ flexGrow: 1, position: 'relative' }}>
        {fixMutation.isLoading ? (
          <Box sx={{ py: 1, textAlign: 'center' }}>
            <Typography variant="caption" color="text.secondary">FIXING...</Typography>
          </Box>
        ) : consistencyQuery.isLoading ? (
          <Box sx={{ py: 1, textAlign: 'center' }}>
            <CircularProgress size={16} />
          </Box>
        ) : consistencyQuery.isError ? (
          <Box sx={{ py: 1, textAlign: 'center' }}>
            <Typography variant="caption" color="error" display="block">ERROR</Typography>
            <Link
              component="button"
              sx={{ fontSize: "0.65rem" }}
              onClick={() => consistencyQuery.refetch()}
            >
              Retry
            </Link>
          </Box>
        ) : consistency ? (
          <Stack direction="row" alignItems="center" spacing={0.5}>
            <Tooltip title={isHealthy ? "All records are consistent" : `${formatNumber(consistency.inconsistentCount, 0)} inconsistent records — click to view`} arrow>
              <Box
                onClick={isHealthy ? undefined : handleNavigateToInconsistent}
                sx={{
                  py: 0.75,
                  px: 1,
                  borderRadius: 2,
                  bgcolor: isHealthy ? "success.main" : "error.main",
                  flexGrow: 1,
                  display: 'flex',
                  flexDirection: 'row',
                  alignItems: 'center',
                  justifyContent: 'flex-start',
                  gap: 0.75,
                  ...(!isHealthy && {
                    cursor: 'pointer',
                    '&:hover': { opacity: 0.85 },
                  }),
                }}
              >
                {isHealthy ? <CheckCircle sx={{ fontSize: 22, color: 'white' }} /> : <Warning sx={{ fontSize: 22, color: 'white' }} />}
                <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-start' }}>
                  <Typography
                    variant="caption"
                    component="div"
                    sx={{
                      fontWeight: "bold",
                      color: 'white',
                      fontSize: "0.75rem",
                      lineHeight: 1.1,
                    }}
                  >
                    {formatNumber(consistency.consistencyPercentage, 0)}%
                  </Typography>
                  <Typography variant="caption" sx={{ color: 'white', fontSize: '0.7rem', fontWeight: 500, lineHeight: 1.1 }}>
                    ({formatNumber(consistency.consistentCount, 0)}/{formatNumber(consistency.totalCount, 0)})
                  </Typography>
                </Box>
              </Box>
            </Tooltip>
            {!isHealthy && (
              <IconButton
                size="small"
                onClick={() => fixMutation.mutate()}
                disabled={fixMutation.isLoading}
                title="Fix inconsistencies"
              >
                <AutoFixNormalIcon sx={{ fontSize: 16 }} />
              </IconButton>
            )}
          </Stack>
        ) : null}
      </Box>
    </Stack>
  );
}
