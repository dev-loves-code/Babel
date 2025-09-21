import React from "react";
import "./ReturnRequestsTable.css";

interface ReturnRequest {
  id: number;
  borrowId: number;
  bookTitle: string;
  requestDate: string;
  status: number;
  isOverdue: boolean;
}

interface ReturnRequestsTableProps {
  returnRequests: ReturnRequest[];
  loading: boolean;
}

const ReturnRequestsTable: React.FC<ReturnRequestsTableProps> = ({
  returnRequests,
  loading,
}) => {
  const statusLabels: Record<number, string> = {
    0: "Pending",
    1: "Approved",
    2: "Declined",
  };

  if (loading)
    return (
      <div className="return-requests-section">
        Loading your return requests...
      </div>
    );

  return (
    <div className="return-requests-section">
      <h2>My Return Requests</h2>
      {returnRequests.length === 0 ? (
        <div>You have no return requests.</div>
      ) : (
        <table className="return-requests-table">
          <thead>
            <tr>
              <th>Book Title</th>
              <th>Request Date</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            {returnRequests.map((request) => (
              <tr key={request.id}>
                <td data-label="Book Title">{request.bookTitle}</td>
                <td data-label="Request Date">
                  {new Date(request.requestDate).toLocaleDateString()}
                </td>
                <td data-label="Status" className={`status-${request.status}`}>
                  {statusLabels[request.status] ?? "Unknown"}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
};

export default ReturnRequestsTable;
