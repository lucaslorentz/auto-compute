import React from "react";
import { createRoot } from "react-dom/client";
import { HashRouter } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "react-query";
import { LocalizationProvider } from "@mui/x-date-pickers";
import { AdapterDateFns } from "@mui/x-date-pickers/AdapterDateFnsV3";
import { CssBaseline } from "@mui/material";
import App from "./App";
import { loadConfig } from "./api";
import { HashRouterNuqsAdapter } from "./HashRouterNuqsAdapter";

import { ThemeProvider } from "@mui/material/styles";
import { theme } from "./theme";

import "@fontsource/roboto/300.css";
import "@fontsource/roboto/400.css";
import "@fontsource/roboto/500.css";
import "@fontsource/roboto/700.css";

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: false,
    },
  },
});

const root = createRoot(document.getElementById("root")!);

loadConfig().then(() => {
  root.render(
    <React.StrictMode>
      <QueryClientProvider client={queryClient}>
        <ThemeProvider theme={theme}>
          <LocalizationProvider dateAdapter={AdapterDateFns}>
            <HashRouter>
              <HashRouterNuqsAdapter>
                <CssBaseline />
                <App />
              </HashRouterNuqsAdapter>
            </HashRouter>
          </LocalizationProvider>
        </ThemeProvider>
      </QueryClientProvider>
    </React.StrictMode>
  );
});
