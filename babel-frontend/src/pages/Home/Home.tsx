import React from "react";
import "./Home.css";
import About from "../../components/About/About";

const Home: React.FC = () => {
  return (
    <div>
      <div id="home" className="home-container">
        <div className="overlay">
          <div className="text-background">
            <h1 className="main-heading animated-text">BABEL</h1>
            <p className="sub-heading animated-text">
              Where Literature Meets Timeless Elegance
            </p>
          </div>
        </div>
      </div>
      <About />
    </div>
  );
};

export default Home;
