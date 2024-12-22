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
  { key: "Status", label: "Status" },
  { key: "Playtime", label: "Playtime" },
  { key: "Progress", label: "Progress" },
];

export const MyMediaColumns = [
  { key: "Status", label: "Status" },
  { key: "My Playtime", label: "My Playtime" },
  { key: "Progress", label: "Progress" },
];
