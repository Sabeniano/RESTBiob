import { applyMiddleware, createStore, compose } from 'redux';
import { loadUser } from "redux-oidc";
import { createBrowserHistory } from "history";
import { routerMiddleware } from 'connected-react-router'
import { createLogger } from 'redux-logger'
import reducer from "../reducers/Index";
import userManager from "../Components/login/UserManager";

const history = createBrowserHistory()

const defaultState = {};

const logger = createLogger({
    collapsed: true
});

const createStoreWithMiddleware = compose(
  applyMiddleware(logger, routerMiddleware(history))
)(createStore);

var store = createStoreWithMiddleware(reducer(history), defaultState);
loadUser(store, userManager);

export default store;