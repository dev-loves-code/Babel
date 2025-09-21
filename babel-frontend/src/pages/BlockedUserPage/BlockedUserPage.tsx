import React from "react";
import "./BlockedUserPage.css";

const BlockedUserPage: React.FC = () => {
  return (
    <div className="blocked-user-page">
      <div className="blocked-user-container">
        <div className="blocked-icon">⛔</div>
        <h1>Account Restricted</h1>
        <p>Your account has been temporarily suspended.</p>
        <p>Please contact the administrator for assistance.</p>
        <div className="contact-info">
          <p>Email: admin@babel-library.com</p>
          <p>Phone: +1 (555) 123-4567</p>
        </div>
      </div>
    </div>
  );
};

export default BlockedUserPage;
