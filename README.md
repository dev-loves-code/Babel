# 📚 Babel – E-Library Platform

Babel is a full-stack **e-library web application** built with a **.NET backend** and a **React TypeScript frontend**.  
It provides users with a modern online library experience including book borrowing, wishlists, notifications, and admin management tools.  

---

## 🚀 Features

### 👤 User
- Borrow up to **3 books simultaneously**
- Manage **wishlist**
- Update **email/password**
- Book **filtering & searching**
- Send **return requests** for borrowed books
- Automatic **blocking** after 2 overdue books (via Hangfire recurring tasks)
- **Welcome email** upon registration (fire-and-forget with MailKit)
- **Weekly summary PDF report** (QuestPDF + MailKit recurring task)
- Real-time **notifications** via SignalR (borrow / return confirmation)

### 🛠️ Admin
- Full **CRUD** for books, genres, and authors
- **Block / unblock users**
- Accept or directly process **return requests**  
  - Accepting creates a **delayed job** (to simulate server load)
- Advanced **filtering & searching**

---

## 🏗️ Architecture
- **Controller – Service – Repository** layered architecture
- **Authentication & Authorization:** Identity + JWT
- **Caching:** Redis
- **Logging:** Serilog + Seq
- **Background Jobs:** Hangfire
- **Emails:** MailKit
- **PDF Reports:** QuestPDF
- **Real-time Communication:** SignalR

---

## 🛠️ Tech Stack

### Backend
- .NET (ASP.NET Core Web API)
- Entity Framework Core (SQL Server)
- Redis
- Seq
- Hangfire
- Serilog
- SignalR
- MailKit
- QuestPDF

### Frontend
- React + TypeScript
- React Router
- Axios
- Protected Routes
- SignalR client
- Lucide React (icons)

---

## ⚙️ Running the Project

### Prerequisites
- [.NET SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/)
- [Docker](https://www.docker.com/)

### 1. Clone the repository
```bash
git clone https://github.com/dev-loves-code/Babel.git
````

### 2. Start infrastructure services

```bash
cd api
docker-compose up -d
```

This will start:

* **Redis** (caching)
* **Seq** (logging dashboard)

### 3. Run the backend

```bash
cd api
dotnet restore
dotnet run
```

### 4. Run the frontend

```bash
cd babel-frontend
npm install
npm run dev
```
