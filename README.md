# RecipeBook

## Overview
👨‍🍳 **RecipeBook** is a full-stack web application for sharing, managing, and discovering cooking recipes.  
It provides a modern, social, and personalized platform that combines usability, interactivity, and clean design.


## Tech Stack
- ASP.NET Core MVC (.NET 6)
- C#
- Entity Framework Core
- SQL Server
- HTML, CSS, JavaScript


## Features

### Recipe Management
- Create, edit, and delete recipes  
- Add ingredients, instructions, categories, and images  
- Full CRUD functionality  

### Browsing & Filtering
- Browse recipes by category  
- Filter by ingredients and popularity  
- Discover trending content  

### Social Features
- Like and comment on recipes  
- Save favorite recipes  
- Follow other users  

### Personalization
- Personalized recommendations based on user activity  
- “Recommended for you” section  

### Authentication & Authorization
- Secure user authentication (ASP.NET Core Identity)  
- Role-based access control:
  - Guests – browse content  
  - Users – create and interact  
  - Admins – full system control  

## Architecture
- Built using **MVC (Model-View-Controller)** pattern  
- Clear separation of concerns  
- Service layer for:
  - Business logic  
  - Recipe management  
  - User management  
  - PDF generation  
  - Statistics  

## Key Functionalities
- Full CRUD operations  
- PDF export of recipes  
- User interactions (likes, favorites, views)  
- Admin dashboard with statistics and analytics  

## Database
- Relational database design  
- Manages users, recipes, ingredients, and interactions  
- Integrated with Entity Framework Core  

## Testing
- Unit and functional testing  
- Ensures application stability and reliability  

