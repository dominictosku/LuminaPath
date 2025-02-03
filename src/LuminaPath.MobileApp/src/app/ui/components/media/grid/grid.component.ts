import { Component, OnInit } from '@angular/core';
import { addIcons } from "ionicons";
import { GridGameDataComponent } from "../data/grid/grid-game-data/grid-game-data.component";

@Component({
    selector: 'app-grid',
    templateUrl: './grid.component.html',
    styleUrls: ['./grid.component.scss'],
    imports: [GridGameDataComponent]
})
export class GridComponent implements OnInit {

  constructor() { }

  ngOnInit() { }

}
