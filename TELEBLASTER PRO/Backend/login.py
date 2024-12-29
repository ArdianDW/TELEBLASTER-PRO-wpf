import sys
import sqlite3
from telethon import TelegramClient
from telethon.errors import SessionPasswordNeededError, PhoneNumberInvalidError
import os

api_id = 26405027
api_hash = '4f40be06c1b180ce5484658e93cf10c3'

def get_db_path():
    app_data_path = os.environ['LOCALAPPDATA']
    return os.path.join(app_data_path, "TELEBLASTER_PRO", "teleblaster.db")

def get_session_path(session_name):
    app_data_path = os.environ['LOCALAPPDATA']
    session_dir = os.path.join(app_data_path, "TELEBLASTER_PRO", "sessions")
    return os.path.join(session_dir, session_name)

def get_account_details(session_name):
    conn = sqlite3.connect(get_db_path())
    cursor = conn.cursor()
    cursor.execute("SELECT phone_number FROM user_sessions WHERE session_name = ?", (session_name,))
    result = cursor.fetchone()
    conn.close()
    return result[0] if result else None

def update_account_status(session_name, status):
    conn = sqlite3.connect(get_db_path())
    cursor = conn.cursor()
    cursor.execute("UPDATE user_sessions SET status = ? WHERE session_name = ?", (status, session_name))
    conn.commit()
    conn.close()

async def main(session_name):
    phone_number = get_account_details(session_name)
    if not phone_number:
        print(f"No account found for session: {session_name}")
        return

    session_path = get_session_path(session_name)
    client = TelegramClient(session_path, api_id, api_hash)
    try:
        await client.start(phone=lambda: phone_number)
        if not await client.is_user_authorized():
            print("User is not authorized. Please check the phone number and try again.")
            return

        print(f"Logged in successfully with session: {session_name}")
        update_account_status(session_name, 1)  # Set status to Active
    except PhoneNumberInvalidError:
        print("The phone number is invalid. Please check and try again.")
    except SessionPasswordNeededError:
        print("Two-step verification is enabled. Please provide the password.")
    except Exception as e:
        print(f"An error occurred: {e}")
    finally:
        await client.disconnect()

if __name__ == "__main__":
    if len(sys.argv) != 2:
        print("Usage: python login.py <session_name>")
    else:
        import asyncio
        asyncio.run(main(sys.argv[1]))
    