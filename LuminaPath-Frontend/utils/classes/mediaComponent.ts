import { DefineComponent } from "nuxt/dist/app/compat/capi";
export class MediaComponent {
  Forms: DefineComponent<{}, {}, any>;
  TableData: DefineComponent<{}, {}, any>;
  TableColumns: Array<Object>;

  constructor(
    forms: DefineComponent<{}, {}, any>,
    tableData: DefineComponent<{}, {}, any>,
    tableColumns: Array<Object>
  ) {
    this.Forms = forms;
    this.TableData = tableData;
    this.TableColumns = tableColumns;
  }
}

export const MediaColumns = [
  { key: "status", label: "Status" },
  { key: "platform", label: "Plattform" },
  { key: "playtime", label: "Playtime" },
  { key: "users", label: "Users" },
  { key: "progress", label: "Progress" },
];

export const MyMediaColumns = [
  { key: "status", label: "Status" },
  { key: "platform", label: "Plattform" },
  { key: "playtime", label: "Playtime" },
  { key: "users", label: "Users" },
  { key: "progress", label: "Progress" },
];
