import './PostMovie.css';
import React, { Component } from 'react';
import axios from 'axios';

class PostMovie extends Component {
  constructor(props) {
    super(props)
    this.state = {
      movieTitle: '',
      movieDescription: '',
      movieRuntime: '',
      moviePoster: '',
      movieProducer: '',
      movieActors: '',
      movieGenre: '',
      movieRelease: '',
      movie3D: '',
      movieRestriction: ''
    };

    this.onChange = this.onChange.bind(this);
    this.onSubmit = this.onSubmit.bind(this);
  }

  onChange(e) {
    this.setState({[e.target.className]: e.target.value});
  }

  onSubmit(e) {
    e.preventDefault();

    const post = {
      title: this.state.movieTitle,
      description: this.state.movieDescription,
      length: this.state.movieRuntime,
      poster: this.state.moviePoster,
      poducer: this.state.movieProducer,
      actors: this.state.movieActors,
      genre: this.state.movieGenre,
      released: this.state.movieRelease,
      threeDee: this.state. movie3D,
      ageRestriction: this.state.movieRestriction
    }

    axios.get('https://localhost:44390/api/v1/movies', {
      method: 'POST',
      headers: {
        'content-type':'application/json'
      },
      body: JSON.stringify(post)
    })
    .then(res => res)
    .then(data => console.log(data));
  }

  render() {
    return (
      <div>
        <h1>Add Movie</h1>
        <form onSubmit={this.onSubmit}>
          <div>
            <label> Title: </label>
            <br/>
            <input type="text" className="movieTitle" onChange={this.onChange} value={this.state.movieTitle}/>
          </div>
          <div>
            <label> Description: </label>
            <br/>
            <textarea className="movieDescription" onChange={this.onChange} value={this.state.movieDescription}/>
          </div>
          <div>
            <label> Runtime: </label>
            <br/>
            <input type="text" className="movieRuntime" onChange={this.onChange} value={this.state.movieRuntime}/>
          </div>
          <div>
            <label> Poster: </label>
            <br/>
            <input type="text" className="moviePoster" onChange={this.onChange} value={this.state.moviePoster}/>
          </div>
          <div>
            <label> Producer: </label>
            <br/>
            <input type="text" className="movieProducer" onChange={this.onChange} value={this.state.movieProducer}/>
          </div>
          <div>
            <label> Actors: </label>
            <br/>
            <input type="text" className="movieActors" onChange={this.onChange} value={this.state.movieActors}/>
          </div>
          <div>
            <label> Genre: </label>
            <br/>
            <input type="text" className="movieGenre" onChange={this.onChange} value={this.state.movieGenre}/>
          </div>
          <div>
            <label> Release Date: </label>
            <br/>
            <input type="text" className="movieRelease" onChange={this.onChange} value={this.state.movieRelease}/>
          </div>
          <div>
            <label> 3D: </label>
            <br/>
            <input type="checkbox" className="movie3D" onChange={this.onChange} value={this.state.movie3D}/>
          </div>
          <div>
            <label> Age Restriction: </label>
            <br/>
            <input type="text" className="movieRestriction" onChange={this.onChange} value={this.state.movieRestriction}/>
          </div>
          <button type="submit">Submit</button>
        </form>
      </div>
    );
  }
}
export default PostMovie;