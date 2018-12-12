import { FETCH_MOVIES, NEW_MOVIES } from './types';

export const fetchMovies = () => dispatch => {
  fetch('https://localhost:44390/api/v1/movies')
  .then(movies =>
    dispatch({
      type: FETCH_MOVIES,
      payload: movies
    })
  );
};