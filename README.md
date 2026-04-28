# 🚀 Axel UI Auto-Builder

**Stop wasting hours on RectTransforms. Let Axel build your UI for you.**

Axel UI Auto-Builder is an AI-powered Unity workflow designed to automate the most tedious parts of UI development. It converts design mockups into pixel-perfect Unity hierarchies in seconds using LLM-generated data, smart sprite mapping, and automated asset processing.

---

## ✨ Key Features

*   **AI-to-Hierarchy**: Generate entire UI Canvases from a single JSON block (compatible with Claude, Gemini, and ChatGPT).
*   **Axel Persona**: Includes a pre-configured AI Persona (`Axel`) that understands Unity's coordinate systems and naming conventions.
*   **Smart Sprite Mapping**: Automatically finds and assigns sprites from your project based on object names or specific `spriteName` tags.
*   **Auto-Text Inference**: Automatically generates TextMeshPro labels for buttons and banners using the `_txt` naming convention.
*   **Bulk Sprite Processor**: One-click import settings for entire folders of UI assets (Sprite type, RGBA32, no mipmaps).
*   **Native Size Support**: Ensures UI elements maintain their intended proportions automatically.

---

## 🛠 Installation

### Via Git URL
1. Open the **Unity Package Manager** (`Window > Package Manager`).
2. Click the **+** icon and select **Add package from git URL...**
3. Paste the following URL:
   `https://github.com/YOUR_USERNAME/axel-ui-autobuilder.git`

---

## 🏎️ The "Axel" Workflow

### 1. Prepare your Assets
Right-click any folder of sprites in your project and select **RUSH-Xtreme > Process UI Sprites**. Axel will instantly set the correct compression and import settings.

### 2. Get the Layout (Meet Axel)
Upload your UI mockup to your favorite AI (Claude/GPT) along with the **`Axel_UI_Expert.md`** file found in this package. Ask him to "Analyze this mockup."

### 3. Build in Unity
1. Go to **Tools > Universal UI > Auto-Builder Pro**.
2. Drag your **Default TMP Font** into the slot.
3. Paste the JSON provided by the AI into the text area.
4. Click **Build & Map Assets**.

---

## 🤖 The AI Persona
This package includes **Axel**, a specialized AI persona designed to act as your Senior UI Architect. By using the rules defined in `Axel_UI_Expert.md`, any LLM becomes an expert at translating visual designs into the exact data your Unity project needs.

---

## 📝 License
This project is licensed under the MIT License.
