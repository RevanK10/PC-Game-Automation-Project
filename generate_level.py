import os
import json
import sys
from google import genai
from google.genai import types

# 1. Initialize the Gemini Client using the GitHub secret environment variable
api_key = os.environ.get("GEMINI_API_KEY")
if not api_key:
    print("❌ ERROR: GEMINI_API_KEY environment variable is missing.")
    sys.exit(1)

client = genai.Client(api_key=api_key)

def generate_level_data(prompt_intent: str):
    print(f"🧠 Prompting Gemini with intent: '{prompt_intent}'...")
    
    prompt = f"""
    You are an expert Game Director AI. Generate a balanced arcade level dataset for a 3D spear-throwing game based on this request: "{prompt_intent}".
    You must output a single JSON object matching this structure exactly. Do not include markdown formatting or wrappers like ```json.
    
    Expected Structure: {{
        "level_id": "string uniquely identifying this configuration",
        "difficulty_name": "creative name for this difficulty",
        "is_boss_level": boolean,
        "waves": [
            {{
                "name": "Enemy Name",
                "health": float between 50.0 and 500.0,
                "speed": float between 1.5 and 6.0,
                "scene_path": "res://enemies/base_enemy.tscn",
                "scale_uniform": float between 1.0 and 3.0,
                "scale_y": float between 1.0 and 3.0,
                "exp_reward": integer between 10 and 100
            }}
        ]
    }}
    """

    response = client.models.generate_content(
        model='gemini-2.5-flash',
        contents=prompt,
        config=types.GenerateContentConfig(
            response_mime_type="application/json"
        ),
    )
    return json.loads(response.text)

def validate_level(data: dict) -> bool:
    print("🛡️ Running inline balance validation checks...")
    
    # 1. Calculate total EXP
    total_exp = sum(enemy['exp_reward'] for enemy in data['waves'])
    if total_exp < 30:
        print("❌ FAIL: Total EXP pool is too low for character progression.")
        return False
        
    # 2. Prevent absurdly fast/tanky AI variables from breaking your game mechanics
    for enemy in data['waves']:
        if enemy['speed'] > 6.0:
            print(f"❌ FAIL: {enemy['name']} is moving too fast ({enemy['speed']}) for player physics handling.")
            return False
        if enemy['health'] > 500.0:
            print(f"❌ FAIL: {enemy['name']} has unkillable amounts of health ({enemy['health']}).")
            return False
            
    print("✅ SUCCESS: Level configuration passed balance check.")
    return True

def main():
    # Read the text prompt file committed to the repository
    prompt_file = "prompt_intent.txt"
    if not os.path.exists(prompt_file):
        print(f"❌ ERROR: Missing target text prompt file: {prompt_file}")
        sys.exit(1)
        
    with open(prompt_file, "r") as f:
        player_intent = f.read().strip()

    level_json = generate_level_data(player_intent)
    
    if validate_level(level_json):
        # Target your exact local Godot resource path path inside your repo
        output_path = "level_config.json"
        with open(output_path, "w") as f:
            json.dump(level_json, f, indent=2)
        print(f"🎉 Successfully saved data file to: {output_path}")
    else:
        print("❌ Level validation failed. Aborting deployment script.")
        sys.exit(1)

if __name__ == "__main__":
    main()
