import React, { useState } from "react";
import { NavLink } from "react-router-dom";
import {
  BookOpen,
  Facebook,
  Twitter,
  Instagram,
  MapPin,
  Phone,
  Mail,
  Clock,
} from "lucide-react";
import "./Footer.css";

const Footer: React.FC = () => {
  const [activeAnchor, setActiveAnchor] = useState<string>("");

  const handleAnchorClick = (anchor: string) => {
    setActiveAnchor(anchor);
  };

  return (
    <footer className="footer">
      <div className="footer-content">
        {/* Brand Section */}
        <div className="footer-section brand-section">
          <div className="brand-header">
            <BookOpen className="logo-icon" />
            <div>
              <h3>BABEL</h3>
              <p>Literary Sanctuary</p>
            </div>
          </div>
          <p className="brand-text">
            A curated sanctuary where timeless literature meets elegant
            discovery. Each shelf tells a story, blending classical wisdom with
            modern insight.
          </p>
          <div className="socials">
            <Facebook size={20} />
            <Twitter size={20} />
            <Instagram size={20} />
          </div>
        </div>

        {/* Quick Links Section */}
        <div className="footer-section">
          <h4>Quick Links</h4>
          <ul className="footer-links">
            <li>
              <NavLink
                to="/"
                className={({ isActive }) => (isActive ? "active-link" : "")}
              >
                Home
              </NavLink>
            </li>

            <li>
              <NavLink
                to="/books"
                className={({ isActive }) => (isActive ? "active-link" : "")}
              >
                Our Collection
              </NavLink>
            </li>
          </ul>
        </div>

        {/* Newsletter & Contact Section */}
        <div className="footer-section newsletter-section">
          <h4>Literary Newsletter</h4>
          <p>
            Discover new arrivals, exclusive events, and literary insights
            delivered to your doorstep.
          </p>
          <form className="newsletter-form">
            <input type="email" placeholder="Enter your email" required />
            <button type="submit">Subscribe</button>
          </form>

          <div className="contact-info">
            <p>
              <MapPin size={16} /> 123 Literary Lane
            </p>
            <p>
              <Phone size={16} /> (555) 123-BOOK
            </p>
            <p>
              <Mail size={16} /> hello@babel-books.com
            </p>
            <p>
              <Clock size={16} /> Mon-Fri: 9AM-8PM
            </p>
          </div>
        </div>
      </div>

      <div className="footer-bottom">
        <p>&copy; 2025 Babel Literary Sanctuary. All rights reserved.</p>
      </div>
    </footer>
  );
};

export default Footer;
