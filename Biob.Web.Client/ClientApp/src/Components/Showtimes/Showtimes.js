import React, { Component } from 'react';
import axios from 'axios';
import './Showtimes.css';

var url = 'https://localhost:44390/api/v1/movies';

// function Showtime(props) {
//     return (
//       <div>{props.showtime}</div>
//     )
// }

class Showtime extends Component {
  state = {
    movieId: this.props.movieId,
    showtimes: []
  }

  componentWillMount() {
    var url2 = 'https://localhost:44390/api/v1/movies/'+ this.state.movieId +'/showtimes/';
    axios.get(url2)
    .then(res => {
      this.setState({showtimes: res.data})
    })
  }

  render(){
    return (
      <div>
        <div>{this.props.showtime}</div>
        <div>{this.state.showtimes.movieId}</div>
        <div>{this.state.showtimes.timeOfPlaying}</div>
      </div>
    )
  }
}

function Movie(props) {
    return (
      <div className="pictureContent">
          <img src={props.movie.poster} alt="" ></img>
          <div className="textContent">
            <h1>{props.movie.title}</h1>
            <Showtime
              movieId = {props.movie.id}
              showtime='test showtime'
            />
          </div>
        </div>
    )
}


// class Movies extends Component {
//     render() {
//       return (
//         <div className="pictureContent">
//             <img src={this.props.movie.poster} alt="" ></img>
//             <div className="textContent">
//               <h1>{this.props.movie.title}</h1>
//               <Showtime
//                 showtime='test showtime'
//               />
//             </div>
//           </div>
//       )
//     }
// }

class Body extends Component {
  state = {
    movies: [],
  };

  componentWillMount() {
    axios.get(url)
    .then(res => {
      this.setState({movies: res.data})
    })
  }

  render() {
    return (
			<div className="wrapper">
        {this.state.movies.map(movie =>
        (<div key={movie.id}>
          <Movie
            movie={movie}
          />
        </div>))}
		  </div>
    )
  }
}

export default Body;