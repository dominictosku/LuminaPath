import { Component, provideZoneChangeDetection, signal } from '@angular/core';
import { bootstrapApplication } from '@angular/platform-browser';
import { provideRouter, RouterLink, RouteReuseStrategy } from '@angular/router';
import { HttpInterceptorFn, HttpResponse, provideHttpClient, withInterceptors } from '@angular/common/http';
import { IonApp, IonRouterOutlet, provideIonicAngular, IonicRouteStrategy } from '@ionic/angular/standalone';
import { delay, of } from 'rxjs';
import { TabsPage } from './app/shared/components/tabs/tabs.page';
import { libraryRoutes } from './app/features/library/library.routes';
import { AuthService } from './app/core/auth/services/auth.service';
import { registerAppIcons } from './app/shared/icons/register-icons';
registerAppIcons();
const game = {id:42,name:'Hades',description:'Escape the underworld, one run at a time.',genre:'Roguelike',platforms:2,playtime:40,releaseDate:'2026-10-01',image:{uri:'http://127.0.0.1:4202/qa-media/hades.jpg'},myGames:{id:7,status:2,timeSpend:12,rating:9,myGameInfo:{trackedHours:0}},dlcs:[]};
const anime = {id:43,name:'Frieren',description:'A journey beyond the end.',genre:'Fantasy',releaseDate:'2026-10-01',image:null,episodeCount:28,expectedWatchTimeMinutes:672,myAnimes:{id:8,status:2,currentEpisode:5}};
const mock: HttpInterceptorFn = req => {
 const path = new URL(req.url,'http://localhost').pathname.replace('/api/','');
 let body:unknown=[];
 if(path==='games/42') body=game;
 else if(path==='animes/43') body=anime;
 else if(['games','animes','movies','series'].includes(path)) {
  const data=path==='games'?[game]:path==='animes'?[anime]:[];
  const filtered=data.filter(x=>x.name.toLowerCase().includes((req.params.get('searchString')??'').toLowerCase()));
  body={data:filtered,totalCount:filtered.length,pageIndex:1,totalPages:1};
 } else if(path==='browse/games/releases') body=[{...game,kind:'games',addedCount:5,libraryEntry:game.myGames}];
 else if(path==='browse/animes/releases') body=[{...anime,kind:'animes',addedCount:5,libraryEntry:anime.myAnimes}];
 else if(path==='quests/board') body={xp:0,currentStreakDays:0,longestStreakDays:0,quests:[],skills:[],achievements:[]};
 else if(path.includes('forecast')) body=null;
 return of(new HttpResponse({body,status:200})).pipe(delay(path.includes('achievements')?1500:150));
};
@Component({selector:'app-root',imports:[IonApp,IonRouterOutlet,RouterLink],template:`<ion-app><nav style="height:50px;position:absolute;top:0;z-index:10;padding:16px;display:flex;gap:24px;background:#111827;width:100%"><a routerLink="/library">Library</a><a routerLink="/browse">Browse</a><span>Isolated UI verification · sample data</span></nav><ion-router-outlet style="top:50px" /></ion-app>`})
class Preview {}
bootstrapApplication(Preview,{providers:[provideZoneChangeDetection(),provideIonicAngular(),{provide:RouteReuseStrategy,useClass:IonicRouteStrategy},provideHttpClient(withInterceptors([mock])),provideRouter([{path:'',component:TabsPage,children:[...libraryRoutes,{path:'browse',loadComponent:()=>import('./app/features/browse/pages/browse.page').then(m=>m.BrowsePage)},{path:'',redirectTo:'library',pathMatch:'full'}]}]),{provide:AuthService,useValue:{canEditCatalog:signal(true),isAdmin:signal(true),isAuthenticated:signal(true),roles:signal(['Admin'])}}]});
