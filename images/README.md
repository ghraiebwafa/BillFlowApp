# Images

This folder holds **static visual assets for documentation** — not runtime application assets.

## What belongs here

| Type | Examples |
|------|----------|
| UI mockups | Mobile and desktop screen designs |
| Architecture diagrams | Service topology, data flow |
| Screenshots | Setup steps, Swagger, deployed app |
| Brand references | Logo variants used in docs |

Application assets that the SPA loads at runtime (logo, icons) live in **`Frontend/public/assets/`**, not here.

## What does not belong here

- `.env` files or secrets
- Database dumps
- Large binary builds
- User-uploaded production data

## Naming convention

Use descriptive, lowercase names with hyphens:

```
invoice-detail-mockup.png
architecture-overview.png
docker-setup-screenshot.png
```

If the image is tied to a doc section, mention the filename in the relevant README.

## Adding images to documentation

Reference images from README files with a relative path:

```markdown
![BillFlow login screen](images/login-mockup.png)
```

Keep file sizes reasonable (compress PNGs when possible) so the repository stays lightweight.
