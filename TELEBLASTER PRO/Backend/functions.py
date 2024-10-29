import asyncio
from telethon import TelegramClient
from telethon.errors import SessionPasswordNeededError

api_id = 26405027
api_hash = '4f40be06c1b180ce5484658e93cf10c3'

async def check_account_login(session_name):
    client = TelegramClient(session_name, api_id, api_hash)
    try:
        await client.connect()
        if not await client.is_user_authorized():
            print(f"Account {session_name} login status: Inactive")
            return False
        print(f"Account {session_name} login status: Active")
        return True
    except Exception as e:
        print(f"Error checking login status for {session_name}: {e}")
        return False
    finally:
        await client.disconnect()

def check_account_login_sync(session_name):
    return asyncio.run(check_account_login(session_name))
