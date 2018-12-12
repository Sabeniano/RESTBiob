import './Movies.css';
import React, { Component } from 'react';
import PropTypes from 'prop-types';
import {Link} from 'react-router-dom';
import { connect } from 'react-redux';
import { fetchMovies } from '../../actions/MoviesActions';

class Movies extends Component {
  componentWillMount() {
    this.props.fetchMovies();
  }

  render() {
    const moviesItems = this.props.movies.map(movies => (
      <div key={movies.id}>
        <div className="pictureContent">
          <img src={movies.poster} alt=""/>
          <div className="textContent">
          <Link to={`/movies/${movies.title.replace(/ /g, "-")}`} className="movieLink">
                {/* <Movie movies={movies}/> */}
                <h1>{movies.title}</h1>
              </Link>
            <p>{movies.description}</p>
          </div>
        </div>
      </div>
      ));
    return (
			<div className="wrapper">
        {moviesItems}
		  </div>
    )
  }
};

Movies.propTypes = {
  fecthMovies: PropTypes.func.isRequired,
  movies: PropTypes.array.isRequired
}

const mapStateToProps = state => ({
  movies: state.movies.items
});

export default connect(mapStateToProps, { fetchMovies })(Movies);