# Installation

To install and set up **TailwindVariants.NET** in your project, follow these steps:

## 1. Install the NuGet Package

Run the following command in your terminal or Package Manager Console:

```bash
dotnet add package TailwindVariants.NET
```

Or, using the NuGet Package Manager in Visual Studio, search for `TailwindVariants.NET` and install it.

## 2. Add Tailwind CSS to Your Project

If you haven't already, set up Tailwind CSS in your Blazor project. You can follow the official Tailwind CSS documentation: [https://tailwindcss.com/docs/installation](https://tailwindcss.com/docs/installation)

## 3. Configure TailwindVariants.NET

After installing the package, register the services in your `Program.cs`:

```csharp
builder.Services.AddTailwindVariants();
```

## 4. Usage Example

TailwindVariants.NET does not include predefined components. To learn how to create a custom component, see the guide: [First Component](docs/first-component)

## 5. Build and Run

Build and run your project to verify the integration.

---

For more advanced configuration and usage, refer to the documentation or examples provided in the repository.