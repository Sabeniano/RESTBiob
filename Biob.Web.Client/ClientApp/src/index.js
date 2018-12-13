import React from 'react';
import ReactDOM from 'react-dom';
import './index.css';
import App from './App';
import { Provider } from 'react-redux';
import store from './Stores/ConfigureStore';
import { OidcProvider } from 'redux-oidc';
import userManager from "./Components/login/UserManager";

ReactDOM.render(<Provider store={store} ><OidcProvider store={store} userManager= {userManager}><App /></OidcProvider></Provider>, document.getElementById('root'));