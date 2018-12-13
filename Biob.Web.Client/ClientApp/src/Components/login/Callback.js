import React from "react";
import { connect } from "react-redux";
import { CallbackComponent } from "redux-oidc";
import userManager from "../login/UserManager";
import { push } from 'connected-react-router'

class CallbackPage extends React.Component {
  render() {
    const successfullCallback = (user) => {
      this.props.dispatch(push('/home'));
    }
    const errorCallback= (error) => {
      this.props.dispatch(push("/home"));
      console.error(error);
    }
    return (
      <CallbackComponent
        userManager={userManager}
        successCallback={successfullCallback}
        errorCallback={errorCallback}
        >
        <div>Redirecting...</div>
      </CallbackComponent>
    );
  }
}

export default connect()(CallbackPage);