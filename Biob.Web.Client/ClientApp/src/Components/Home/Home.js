import './Home.css';
import {Link} from 'react-router-dom';
import React, { Component } from 'react';
import axios from 'axios';


var url = 'https://localhost:44390/api/v1/movies';

class Home extends Component {
  constructor(props) {
    super(props)
    this.state = {
      movies: []
    }
  }

  componentDidMount() {
    axios.get(url)
    .then(res => {
      this.setState({movies: res.data})
    })
  }

  render() {
    const moviesItems = this.state.movies.map(movies => (
      <div key={movies.id}>
        <div className="pictureContent">
          <img src={movies.poster} alt=""/>
          <div className="textContent">
          <Link to={`/movies/${movies.title.replace(/ /g, "-")}`} className="movieLink">
                {/* <Movie movies={movies}/> */}
                <h1>{movies.title}</h1>
              </Link>
            <p className="movieDesc">{movies.description}</p>
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
}
export default Home;