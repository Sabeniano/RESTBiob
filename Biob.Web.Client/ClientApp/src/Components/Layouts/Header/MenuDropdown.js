import "./MenuDropdown.css";
import React, { Component } from 'react';

    class MenuDropdown extends Component {
        
        // handleClick = () => this.props.onClick(this.props.index)
        
        render() {  
            return(
                <div className="menuDropdown">This is unfortunately not a dropdown, or is it?</div>
            )
            
            // return <button
            //     type='button'
            //     className={
            //     this.props.isActive ? 'active' : 'album'
            //     }
            //     onClick={ this.handleClick }
            // >
            //     <span>{ this.props.name }</span>
            // </button>
        }
    }

export default MenuDropdown;