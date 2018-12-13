import "./Login.css";
import React, {Component} from 'react';
import axios from 'axios';
import userManager from './UserManager';
import {Link} from 'react-router-dom';


class Login extends Component
{
  Api()
  {
    userManager.getUser().then(user => {
      let url = 'https://localhost:44393/identity';
      axios.get(url, { headers: { 'Authorization': 'Bearer ' + user.access_token } })
      .then(data => {
        console.log(data.status, JSON.parse(data.data));
      });
    });
  }
  onLogoutButtonClick(event) {
    event.preventDefault();
    userManager.signoutRedirect();
    userManager.removeUser();
  }
  onLoginButtonClick(event) {
    event.preventDefault();
    userManager.signinRedirect();
  }
  render() {
    return (
      <ul className="loginBar">
        <li classname="loginBarButton toolbarItem"><Link to='/Admin'>Admin</Link></li>
        {/* </li>/<li><Link to='/movies' className='navLink toolbarItem'>Movies</Link></li> */}
        <li className="loginBarButton toolbarItem" onClick={this.Api}>Call Api</li>
        <li className="loginBarButton toolbarItem" onClick={this.onLoginButtonClick}>Login</li>
        <li className="loginBarButton toolbarItem" onClick={this.onLogoutButtonClick}>Logout</li>
        <pre className="Results"></pre>
      </ul>
    );
  }
}

export default Login;