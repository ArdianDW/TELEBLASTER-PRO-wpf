import sqlite3
from telethon import TelegramClient, events, sync
from telethon.tl.types import InputPhoneContact
from telethon.tl.functions.contacts import ImportContactsRequest
from telethon.tl.functions.contacts import GetContactsRequest
from telethon.tl.types import InputPeerUser
from telethon.errors import SessionPasswordNeededError, PhoneNumberInvalidError

api_id = 26405027
api_hash = '4f40be06c1b180ce5484658e93cf10c3'

def get_highest_session_name():
    conn = sqlite3.connect('teleblaster.db')  
    cursor = conn.cursor()
    cursor.execute("SELECT session_name FROM user_sessions WHERE session_name LIKE 'user%.session'")
    sessions = cursor.fetchall()
    conn.close()

    if not sessions:
        return 'user1.session'

    highest_number = max(int(session[0][4:-8]) for session in sessions)
    return f'user{highest_number + 1}.session'

session_name = get_highest_session_name()

client = TelegramClient(session_name, api_id, api_hash)

async def main():
    try:
        await client.start()
        if not await client.is_user_authorized():
            print("User is not authorized. Please check the phone number and try again.")
            return

        me = await client.get_me()
        if me:
            save_user_data(me, session_name)
        else:
            print("Failed to retrieve user data.")
    except PhoneNumberInvalidError:
        print("The phone number is invalid. Please check and try again.")
    except SessionPasswordNeededError:
        print("Two-step verification is enabled. Please provide the password.")
    except Exception as e:
        print(f"An error occurred: {e}")
    finally:
        await client.disconnect()

def save_user_data(me, session_name):
    try:
        connection = sqlite3.connect('teleblaster.db', timeout=10)
        cursor = connection.cursor()
        
        cursor.execute('''
            INSERT OR REPLACE INTO user_sessions (session_name, phone_number, telegram_user_id, username, realname, status) VALUES (?, ?, ?, ?, ?, ?)
        ''', (session_name, me.phone, me.id, me.username, f"{me.first_name} {me.last_name}", 1))
        
        connection.commit()
    except sqlite3.Error as e:
        print(f"Database error: {e}")
    finally:
        connection.close()

with client:
    client.loop.run_until_complete(main())
