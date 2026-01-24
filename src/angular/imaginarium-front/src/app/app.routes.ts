import { Routes } from '@angular/router';
import { authGuard, loginGuard } from './guards/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./components/login/login.component').then(m => m.LoginComponent),
    canActivate: [loginGuard]
  },
  {
    path: 'register',
    loadComponent: () => import('./components/register/register.component').then(m => m.RegisterComponent),
    canActivate: [loginGuard]
  },
  {
    path: '',
    loadComponent: () => import('./components/home/home.component').then(m => m.HomeComponent),
    canActivate: [authGuard]
  },
  {
    path: 'albums/create',
    loadComponent: () => import('./components/create-album/create-album.component').then(m => m.CreateAlbumComponent),
    canActivate: [authGuard]
  },
  {
    path: 'albums/:id',
    loadComponent: () => import('./components/album-detail/album-detail.component').then(m => m.AlbumDetailComponent),
    canActivate: [authGuard]
  },
  {
    path: 'groups/:id',
    loadComponent: () => import('./components/group-detail/group-detail.component').then(m => m.GroupDetailComponent),
    canActivate: [authGuard]
  },
  {
    path: 'share/:token',
    loadComponent: () => import('./components/public-share/public-share.component').then(m => m.PublicShareComponent)
  },
  {
    path: '**',
    redirectTo: '/login'
  }
];