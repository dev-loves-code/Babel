import React, { useState, useEffect } from "react";
import { MdOutlineArrowDropDown, MdOutlineArrowDropUp } from "react-icons/md";
import "./About.css";
import data from "./accordion";

const About: React.FC = () => {
  const [expandedItems, setExpandedItems] = useState<number[]>([]);
  const [currentImageIndex, setCurrentImageIndex] = useState<number>(0);

  const images: string[] = [
    "/aboutus/image1.jpg",
    "/aboutus/image2.jpg",
    "/aboutus/image3.jpg",
    "/aboutus/image4.jpg",
  ];

  useEffect(() => {
    const interval = setInterval(() => {
      setCurrentImageIndex((prevIndex) => (prevIndex + 1) % images.length);
    }, 3000);
    return () => clearInterval(interval);
  }, [images.length]);

  useEffect(() => {
    const numbers = document.querySelectorAll<HTMLElement>(".number");

    const animateNumbers: IntersectionObserverCallback = (entries) => {
      entries.forEach((entry) => {
        if (entry.isIntersecting) {
          const updateCount = (element: HTMLElement) => {
            const target = Number(element.getAttribute("data-target"));
            const count = Number(element.innerText);
            const increment = target / 100;

            if (count < target) {
              element.innerText = Math.ceil(count + increment).toString();
              setTimeout(() => updateCount(element), 30);
            } else {
              element.innerText = target.toString();
            }
          };

          numbers.forEach((number) => updateCount(number));
        }
      });
    };

    const observer = new IntersectionObserver(animateNumbers, {
      threshold: 0.5,
    });
    numbers.forEach((number) => observer.observe(number));

    return () => observer.disconnect();
  }, []);

  const handleAccordionClick = (index: number) => {
    setExpandedItems((prev) =>
      prev.includes(index)
        ? prev.filter((item) => item !== index)
        : [...prev, index]
    );
  };

  return (
    <section id="about" className="about-section">
      <div className="about-container">
        <div className="about-content">
          <h2 className="section-title">About Babel</h2>
          <p className="section-subtitle">
            A Sanctuary of Knowledge and Timeless Stories
          </p>
          <div className="about-grid">
            <div className="about-carousel">
              <div
                className="carousel-track"
                style={{
                  transform: `translateX(-${currentImageIndex * 100}%)`,
                }}
              >
                {images.map((src, index) => (
                  <img src={src} alt={`Slide ${index + 1}`} key={index} />
                ))}
              </div>
            </div>
            <div className="about-text">
              <p>
                At Babel, we curate more than books. Each shelf tells a story,
                blending classical wisdom with modern insight. We invite readers
                to explore, reflect, and immerse themselves in literature that
                transcends time.
              </p>
              <p>
                Our dedication to preservation, elegance, and discovery defines
                every interaction. From rare editions to contemporary
                masterpieces, we strive to create a space where every visitor
                uncovers something extraordinary.
              </p>

              <div className="stats-container">
                <div className="stat">
                  <div className="stat-card">
                    <h3 className="number" data-target="1200">
                      0
                    </h3>
                    <p>Volumes Curated</p>
                  </div>
                </div>
                <div className="stat">
                  <div className="stat-card">
                    <h3 className="number" data-target="850">
                      0
                    </h3>
                    <p>Happy Patrons</p>
                  </div>
                </div>
                <div className="stat">
                  <div className="stat-card">
                    <h3 className="number" data-target="25">
                      0
                    </h3>
                    <p>Years of Excellence</p>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Values Section - Custom Accordion */}
        <div className="values-section">
          <h3 className="values-title">Our Core Values</h3>
          <div className="accordion">
            {data.map((item, i) => {
              const isExpanded = expandedItems.includes(i);
              return (
                <div
                  className={`accordionItem ${isExpanded ? "expanded" : ""}`}
                  key={i}
                >
                  <button
                    className="accordionButton"
                    onClick={() => handleAccordionClick(i)}
                  >
                    <div className="accordion-icon">{item.icon}</div>
                    <span className="accordion-title">{item.heading}</span>
                    <div className="accordion-arrow">
                      {isExpanded ? (
                        <MdOutlineArrowDropUp size={24} />
                      ) : (
                        <MdOutlineArrowDropDown size={24} />
                      )}
                    </div>
                  </button>
                  {isExpanded && (
                    <div className="accordionPanel">
                      <p className="accordion-content">{item.detail}</p>
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        </div>
      </div>
    </section>
  );
};

export default About;
