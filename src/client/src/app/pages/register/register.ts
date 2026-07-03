import { Component } from '@angular/core';
import { AuthLayout } from '../../components/auth-layout/auth-layout';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-register',
  imports: [AuthLayout, RouterLink],
  templateUrl: './register.html',
  styleUrl: './register.css',
})
export class Register {}
