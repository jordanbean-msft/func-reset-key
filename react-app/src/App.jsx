import React, { useState } from "react";
import { AuthenticatedTemplate, UnauthenticatedTemplate, useMsal } from "@azure/msal-react";
import { loginRequest, backendApiRequest } from "./authConfig";
import { PageLayout } from "./components/PageLayout";
import { ProfileData } from "./components/ProfileData";
import { callMsGraph } from "./graph";
import { callBackendApi } from "./backendApi";
import Button from "react-bootstrap/Button";
import "./styles/App.css";

const BackendApiContent = () => {
  const { instance, accounts } = useMsal();
  const [backendApiData, setBackendApiData] = useState(null);

  function RequestBackendApiData() {
    // Silently acquires an access token which is then attached to a request for backend API data
    instance.acquireTokenSilent({
      ...backendApiRequest,
      account: accounts[0]
    }).then((response) => {
      callBackendApi(response.accessToken).then(response => setBackendApiData(response));
    });
  }

  return (
    <>
      <h5 className="card-title">Backend API Data</h5>
      {backendApiData ?
        <div>
          <p>{backendApiData.status}</p>
          <p>{backendApiData.data}</p>
        </div>
        :
        <Button variant="secondary" onClick={RequestBackendApiData}>Request BackendApi Data</Button>
      }
    </>
  );
}

/**
 * Renders information about the signed-in user or a button to retrieve data about the user
 */
const ProfileContent = () => {
  const { instance, accounts } = useMsal();
  const [graphData, setGraphData] = useState(null);

  function RequestProfileData() {
    // Silently acquires an access token which is then attached to a request for MS Graph data
    instance.acquireTokenSilent({
      ...loginRequest,
      account: accounts[0]
    }).then((response) => {
      callMsGraph(response.accessToken).then(response => setGraphData(response));
    });
  }

  return (
    <>
      <h5 className="card-title">Welcome {accounts[0].name}</h5>
      {graphData ?
        <ProfileData graphData={graphData} />
        :
        <Button variant="secondary" onClick={RequestProfileData}>Request Profile Information</Button>
      }
    </>
  );
};

/**
 * If a user is authenticated the ProfileContent component above is rendered. Otherwise a message indicating a user is not authenticated is rendered.
 */
const MainContent = () => {
  return (
    <div className="App">
      <AuthenticatedTemplate>
        <ProfileContent />
        <BackendApiContent />
      </AuthenticatedTemplate>

      <UnauthenticatedTemplate>
        <h5 className="card-title">Please sign-in to see your profile information.</h5>
      </UnauthenticatedTemplate>
    </div>
  );
};

export default function App() {
  return (
    <PageLayout>
      <MainContent />
    </PageLayout>
  );
}
