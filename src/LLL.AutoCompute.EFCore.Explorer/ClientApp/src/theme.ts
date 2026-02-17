import { createTheme } from "@mui/material/styles";

const fontSize = 13;
const scale = fontSize / 14;

export const theme = createTheme({
  spacing: 8,
  typography: {
    fontSize: fontSize,
    h1: {
      fontSize: `${(scale * 28) / 16}rem`,
    },
    h2: {
      fontSize: `${(scale * 24.5) / 16}rem`,
    },
    h3: {
      fontSize: `${(scale * 22) / 16}rem`,
    },
    h4: {
      fontSize: `${(scale * 20) / 16}rem`,
    },
    h5: {
      fontSize: `${(scale * 18.5) / 16}rem`,
    },
    h6: {
      fontSize: `${(scale * 15) / 16}rem`,
    },
  },
  components: {
    MuiTable: {
      defaultProps: {
        size: "small",
      },
    },
    MuiFormControl: {
      defaultProps: {
        size: "small",
      },
    },
    MuiIconButton: {
      defaultProps: {
        size: "small",
      },
    },
    MuiSvgIcon: {
      defaultProps: {
        fontSize: "small",
      },
    },
    MuiChip: {
      defaultProps: {
        size: "small",
      },
    },
    MuiTypography: {
      styleOverrides: {
        h4: {
          fontWeight: "bold",
        },
        h5: {
          fontWeight: "bold",
        },
      },
    },
    MuiLink: {
      defaultProps: {
        underline: "hover",
      },
      styleOverrides: {
        root: {
          fontSize: `${(scale * 15) / 16}rem`,
        },
      },
    },
    MuiButton: {
      defaultProps: {
        size: "small",
        disableElevation: true,
        variant: "outlined",
      },
      styleOverrides: {
        root: {
          borderRadius: 0,
        },
      },
    },
    MuiTableRow: {
      defaultProps: {
        hover: true,
      },
    },
    MuiPaper: {
      defaultProps: {
        variant: "outlined",
      },
      styleOverrides: {
        root: {
          borderRadius: 0,
        },
      },
    },
    MuiTableHead: {
      styleOverrides: {
        root: {
          backgroundColor: "#f5f5f5",
        },
      },
    },
    MuiTableCell: {
      styleOverrides: {
        root: {
          borderRight: "1px solid rgba(224, 224, 224, 1)",
        },
        head: {
          fontWeight: "bold",
        },
      },
    },
    MuiAutocomplete: {
      styleOverrides: {
        root: {
          "& .MuiOutlinedInput-root": {
            padding: 0,
            "& .MuiAutocomplete-input": {
              padding: "8.5px 12px",
            },
          },
        },
      },
    },
    MuiOutlinedInput: {
      styleOverrides: {
        root: {
          borderRadius: 0,
        },
        adornedStart: {
          paddingLeft: 12,
        },
        adornedEnd: {
          paddingRight: 12,
        },
        input: {
          padding: "8.5px 12px",
        },
        inputAdornedStart: {
          paddingLeft: 0,
        },
        inputAdornedEnd: {
          paddingRight: 0
        }
      },
    },
    MuiCssBaseline: {
      styleOverrides: {
        summary: {
          cursor: "pointer",
        },
      },
    },
  },
});
