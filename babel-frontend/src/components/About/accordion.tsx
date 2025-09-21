import { HiShieldCheck } from "react-icons/hi";
import { MdCancel, MdAnalytics } from "react-icons/md";

interface AccordionData {
  icon: JSX.Element;
  heading: string;
  detail: string;
}

const data: AccordionData[] = [
  {
    icon: <HiShieldCheck />,
    heading: "A Curated Collection of Literary Masterpieces",
    detail:
      "From classical works to rare modern editions, our library offers an exquisite selection designed to inspire and enlighten every reader.",
  },
  {
    icon: <MdCancel />,
    heading: "Transparent & Timeless Access",
    detail:
      "No hidden rules or restrictions; every visitor enjoys equal opportunity to explore our treasures at their own pace.",
  },
  {
    icon: <MdAnalytics />,
    heading: "A Haven for Knowledge Seekers",
    detail:
      "Babel provides an environment of focus, reflection, and discovery, allowing patrons to immerse themselves in literature without distraction.",
  },
];

export default data;
export type { AccordionData };
