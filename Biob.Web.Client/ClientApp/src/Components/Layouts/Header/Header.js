import './Header.css';
import React, { Component } from 'react';
import {Link} from 'react-router-dom';
import Login from '../../login/Login';
import MenuDropdown from './MenuDropdown';


class NavBar extends Component {
	state = {
    activeIndex: null
	}
	handleClick = (index) => this.setState({ activeIndex: index })
  // constructor() {
	// 	super();
    // {
    // this.state = {
    //   showMenuDropdown: false,
    // }
    
    // this.showMenuDropdown = this.showMenuDropdown.bind(this);
	//}

	onBurgerMenuClick(event) {
		event.preventDefault();
		//Todo: Make dropdown appear somehow

		// this.setState({
			// showMenuDropdown: true,
		//});
		//this.className 
		// event.onClick = function(props) {
			
		// {this.className == "fa fa-bars" 
		// 	? (this.className = "fa fa-bars hidden")
		// 	: (this.className = "fa fa-bars") 
	}
	
	
  render() {
    return (
    <header>

			<div className="topNavContainer">
				{/* <div className="topLogo"> */}
					<img className="topLogo" src="https://d2gg9evh47fn9z.cloudfront.net/800px_COLOURBOX6980911.jpg" alt="BIOB"/>
				{/* </div> */}

				<nav>
          {/* <div className="toggle">
            <i className ="fa fa-bars" aria-hidden="true"></i>
          </div> */}
					<ul>

						<li><Link to='/' className='navLink toolbarItem'>Home</Link></li>
						<li><Link to='/showtimes' className='navLink toolbarItem'>Showtimes</Link></li>
						<li><Link to='/movies' className='navLink toolbarItem'>Movies</Link></li>
					</ul>
					<Login />
					<div className="toggle"> 
						<i className ="fa fa-bars" aria-hidden="true" 
						onClick={this.onBurgerMenuClick}
						// index={0}
						// isActive={ this.state.activeIndex===0 }
						// onClick={ this.handleClick }
						>
						
						</i>
					</div>

					<MenuDropdown />
					{/* {
						this.state.showMenuDropdown
						?	(
							
						)
						: (
							null
						)
					} */}
				</nav>
			</div>
		</header>
    )
  }
}
export default NavBar;