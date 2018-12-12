import { combineReducers } from 'redux';
import { reducer as oidcReducer } from 'redux-oidc';
import { connectRouter } from 'connected-react-router'
import subscriptionsReducer from './Subscriptions';
import moviesReducer from './MoviesReducer'

const reducer = (history) => combineReducers(
  {
    oidc: oidcReducer,
    router: connectRouter(history),
    subscriptions: subscriptionsReducer,
    movies: moviesReducer
  }
);

export default reducer;