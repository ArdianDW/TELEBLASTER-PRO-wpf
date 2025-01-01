import asyncio
import sqlite3
import re
from telethon.sync import TelegramClient
from telethon import TelegramClient
from telethon.errors import SessionPasswordNeededError
from telethon.tl.functions.messages import GetDialogsRequest
from telethon.tl.functions.messages import GetFullChatRequest
from telethon.tl.types import InputPeerEmpty, Channel, Chat, User
from telethon.tl.functions.channels import GetParticipantsRequest
from telethon.tl.types import ChannelParticipantsSearch, ChatParticipant, ChatParticipantAdmin, ChatParticipantCreator
from telethon.tl.functions.contacts import GetContactsRequest
from telethon.errors import FloodWaitError
from telethon.tl.functions.messages import CheckChatInviteRequest, ImportChatInviteRequest
from telethon.tl.functions.contacts import ResolveUsernameRequest
from telethon.tl.types import ChatInviteAlready, ChatInvite
from telethon import errors
import time
from telethon.tl.functions.channels import JoinChannelRequest
import os
import random
from telethon.tl.functions.channels import InviteToChannelRequest
from telethon.tl.functions.messages import AddChatUserRequest
from telethon.tl.functions.messages import SendMessageRequest
from telethon.tl.functions.messages import SendMediaRequest
from telethon.tl.types import InputMediaUploadedDocument
from telethon.tl.types import DocumentAttributeFilename
import mimetypes


api_id = 26405027
api_hash = '4f40be06c1b180ce5484658e93cf10c3'

stop_extraction = False

def get_db_path():
    app_data_path = os.environ['LOCALAPPDATA']
    return os.path.join(app_data_path, "TELEBLASTER_PRO", "teleblaster.db")

def get_session_path(session_name):
    app_data_path = os.environ['LOCALAPPDATA']
    session_dir = os.path.join(app_data_path, "TELEBLASTER_PRO", "sessions")
    return os.path.join(session_dir, session_name)

def check_account_login_sync(session_name):
    session_path = get_session_path(session_name)
    client = TelegramClient(session_path, api_id, api_hash)
    try:
        client.connect()
        if not client.is_user_authorized():
            print(f"Status login akun {session_name}: Tidak Aktif")
            return False
        print(f"Status login akun {session_name}: Aktif")
        return True
    except Exception as e:
        print(f"Error saat memeriksa status login untuk {session_name}: {e}")
        return False
    finally:
        client.disconnect()


def load_joined_groups(session_name):
    session_path = get_session_path(session_name)
    client = TelegramClient(session_path, api_id, api_hash)
    try:
        client.connect()
        if not client.is_user_authorized():
            print(f"Akun {session_name} tidak terautentikasi.")
            return []

        result = client(GetDialogsRequest(
            offset_date=None,
            offset_id=0,
            offset_peer=InputPeerEmpty(),
            limit=200,
            hash=0
        ))

        groups = [dialog for dialog in result.chats if dialog.megagroup]

        group_data = []
        for group in groups:
            group_info = {
                "group_id": group.id,
                "group_name": group.title,
                "total_member": group.participants_count
            }
            group_data.append(group_info)
            print(f"Group ID: {group.id}, Group Name: {group.title}, Total Members: {group.participants_count}")

        return group_data

    except Exception as e:
        print(f"Error saat memuat grup untuk {session_name}: {e}")
        return []
    finally:
        client.disconnect()
async def extract_groups_and_channels(session_name):
    session_path = get_session_path(session_name)
    client = TelegramClient(session_path, api_id, api_hash)    
    await client.start()
    dialogs = await client.get_dialogs()
    groups_and_channels = [
        {"group_id": dialog.id, "group_name": dialog.name, "total_members": dialog.entity.participants_count if hasattr(dialog.entity, 'participants_count') else 0}
        for dialog in dialogs
        if dialog.is_group or dialog.is_channel
    ]

    with sqlite3.connect(get_db_path(), timeout=10) as conn:
        c = conn.cursor()
        c.execute("SELECT id FROM user_sessions WHERE session_name = ?", (session_name,))
        user_id = c.fetchone()
        if user_id is None:
            raise ValueError(f"User with session_name {session_name} not found")
        user_id = user_id[0]

        c.execute("DELETE FROM joined_groups WHERE user_id = ?", (user_id,))
        for item in groups_and_channels:
            c.execute("INSERT OR REPLACE INTO joined_groups (user_id, group_id, group_name, total_member) VALUES (?, ?, ?, ?)", 
                      (user_id, item["group_id"], item["group_name"], item["total_members"]))

        conn.commit()

    await client.disconnect()
    return groups_and_channels

def extract_groups_and_channels_sync(session_name):
    return asyncio.run(extract_groups_and_channels(session_name))

async def extract_members(session_name, group_id, group_name, notify_callback, status_callback):
    global stop_extraction
    stop_extraction = False 

    session_path = get_session_path(session_name)
    client = TelegramClient(session_path, api_id, api_hash)    
    await client.start()

    try:
        chat = await client.get_entity(group_id)
        print(f"Chat entity: {chat}")

        # Check if the user is an admin or the owner of the channel
        if isinstance(chat, Channel) and chat.broadcast:
            # Allow extraction only if the user is the owner or has admin rights
            if not chat.creator and not chat.admin_rights:
                notify_callback("You are not an admin or the owner of this channel.")
                return

        all_participants = []

        if isinstance(chat, Channel) and (chat.megagroup or chat.broadcast):
            # Logic for both megagroups and channels
            queryKey = ['a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z']
            for key in queryKey:
                offset = 0
                limit = 100
                while True:
                    if stop_extraction:
                        notify_callback("Extraction stopped by user.")
                        break
                    
                    print(f"Fetching participants with key '{key}', offset {offset}, and limit {limit}")
                    participants = await client(GetParticipantsRequest(
                        chat, ChannelParticipantsSearch(key), offset, limit, hash=0
                    ))

                    if not participants.users:
                        break

                    for user in participants.users:
                        try:
                            if chat.megagroup or (chat.broadcast and re.findall(r"\b[a-zA-Z]", user.first_name)[0].lower() == key):
                                all_participants.append(user)
                                status_callback(f"Extracting {len(all_participants)} members...")
                        except Exception as e:
                            print(f"Error processing user: {e}")

                    offset += len(participants.users)
                    print(f"Fetched {len(participants.users)} participants, new offset is {offset}")
                    
                if stop_extraction:
                    break

        elif isinstance(chat, Chat):
            # Logic for basic chats
            full_chat = await client(GetFullChatRequest(chat.id))
            for participant in full_chat.full_chat.participants.participants:
                if stop_extraction:
                    break

                if isinstance(participant, (ChatParticipant, ChatParticipantAdmin, ChatParticipantCreator)):
                    user_id = participant.user_id
                else:
                    print(f"Unknown participant type: {participant}")
                    continue

                try:
                    user = await client.get_entity(user_id)
                    all_participants.append(user)
                    status_callback(f"Extracting {len(all_participants)} members...")
                except Exception as e:
                    print(f"Failed to get entity for user_id {user_id}: {e}")

        # Save extracted members to the database
        conn = sqlite3.connect(get_db_path(), timeout=30)
        c = conn.cursor()

        c.execute("SELECT id FROM user_sessions WHERE session_name = ?", (session_name,))
        user_id = c.fetchone()
        if user_id is None:
            raise ValueError(f"User with session_name {session_name} not found")
        user_id = user_id[0]

        c.execute("DELETE FROM group_members WHERE group_id = ? AND user_id = ?", (group_id, user_id))

        for user in all_participants:
            member_id = user.id
            access_hash = user.access_hash if hasattr(user, 'access_hash') else ""
            first_name = user.first_name or ""
            last_name = user.last_name or ""
            username = user.username or ""

            c.execute(f"INSERT INTO group_members (group_id, user_id, member_id, access_hash, first_name, last_name, username) VALUES (?, ?, ?, ?, ?, ?, ?)",
                      (group_id, user_id, member_id, access_hash, first_name, last_name, username))

        conn.commit()
        conn.close()

    except Exception as e:
        print(f"Error during extraction: {e}")
        raise  # Re-raise the exception to see the full traceback

    finally:
        await client.disconnect()

def extract_members_sync(session_name, group_id, group_name, notify_callback, status_callback):
    asyncio.run(extract_members(session_name, group_id, group_name, notify_callback, status_callback))

def stop_extraction_process():
    global stop_extraction
    stop_extraction = True

def stop_extraction_sync():
    stop_extraction_process()

def extract_contacts(session_name, extractor_user_id):
    session_path = get_session_path(session_name)
    client = TelegramClient(session_path, api_id, api_hash)    
    client.start()

    contacts = client(GetContactsRequest(hash=0))
    conn = sqlite3.connect(get_db_path(), timeout=10)
    c = conn.cursor()

    # Clear existing contacts for the extractor user
    c.execute("DELETE FROM contacts WHERE user_id = ?", (extractor_user_id,))

    for contact in contacts.users:
        user_id = contact.id
        access_hash = contact.access_hash
        first_name = contact.first_name or ""
        last_name = contact.last_name or ""
        username = contact.username or ""

        c.execute("INSERT INTO contacts (user_id, contact_id, access_hash, first_name, last_name, user_name) VALUES (?, ?, ?, ?, ?, ?)",
                  (extractor_user_id, user_id, access_hash, first_name, last_name, username))

    conn.commit()
    conn.close()
    client.disconnect()

def get_group_type(session_name, group_link):
    async def _get_group_type():
        session_path = get_session_path(session_name)
        client = TelegramClient(session_path, api_id, api_hash)
        await client.start()

        try:
            if 'joinchat' in group_link:
                invite_hash = group_link.split('/')[-1]
                invite = await client(CheckChatInviteRequest(invite_hash))
                if isinstance(invite, ChatInviteAlready):
                    chat = invite.chat
                    if isinstance(chat, Channel):
                        if chat.megagroup:
                            return "supergroup"
                        elif chat.broadcast:
                            return "channel"
                        else:
                            return "group"
                    else:
                        return "group"
                elif isinstance(invite, ChatInvite):
                    if invite.megagroup:
                        return "supergroup"
                    elif invite.broadcast:
                        return "channel"
                    else:
                        return "group"
                else:
                    return "invalid"
            else:
                username = group_link.split('/')[-1]
                result = await client(ResolveUsernameRequest(username))
                if result.chats:
                    chat = result.chats[0]
                    if isinstance(chat, Channel):
                        if chat.megagroup:
                            return "supergroup"
                        elif chat.broadcast:
                            return "channel"
                        else:
                            return "group"
                    else:
                        return "group"
                else:
                    return "invalid"
        except errors.InviteHashInvalidError:
            return "invalid"
        except errors.InviteHashExpiredError:
            return "expired"
        except Exception as e:
            return "error"
        finally:
            await client.disconnect()

    return asyncio.run(_get_group_type())

async def join_group_async(session_name, group_link):
    session_path = get_session_path(session_name)
    client = TelegramClient(session_path, api_id, api_hash)
    await client.start()

    try:
        success = False
        group_name = None
        total_member = None

        if 'joinchat' in group_link:
            invite_hash = group_link.split('/')[-1]
            invite = await client(CheckChatInviteRequest(invite_hash))
            chat = invite.chat if isinstance(invite, ChatInviteAlready) else invite
            await client(ImportChatInviteRequest(invite_hash))
            group_name = chat.title
            total_member = chat.participants_count
            success = True
        else:
            username = group_link.split('/')[-1]
            result = await client(ResolveUsernameRequest(username))
            if result.chats:
                chat = result.chats[0]
                await client(JoinChannelRequest(chat))
                group_name = chat.title
                total_member = chat.participants_count
                success = True

        return success, group_name, total_member

    except errors.FloodWaitError as e:
        print(f"Flood wait error: Need to wait for {e.seconds} seconds")
        time.sleep(e.seconds)
        return False, None, None
    except Exception as e:
        print(f"Failed to join: {e}")
        return False, None, None
    finally:
        await client.disconnect()

def join_group(session_name, group_link):
    return asyncio.run(join_group_async(session_name, group_link))

async def validate_phone_number(session, phone_number):
    session_path = get_session_path(session)
    client = TelegramClient(session_path, api_id, api_hash)
    try:
        await client.connect()
        if not await client.is_user_authorized():
            print(f"Akun {session} tidak terautentikasi.")
            return False, None

        print(f"Memvalidasi nomor telepon: {phone_number}")
        user = await client.get_entity(phone_number)
        user_info = {
            "user_id": user.id,
            "access_hash": user.access_hash,
            "username": user.username
        }
        print(f"Nomor valid: {user_info}")
        return True, user_info

    except Exception as e:
        print(f"Error saat memvalidasi nomor {phone_number}: {e}")
        return False, None
    finally:
        await client.disconnect()

def validate_phone_number_sync(session, phone_number):
    return asyncio.run(validate_phone_number(session, phone_number))

async def invite_to_group_async(session_name, group_link, contact_id):
    session_path = get_session_path(session_name)
    client = TelegramClient(session_path, api_id, api_hash)
    await client.start()

    try:
        # Resolve the group entity (chat or channel)
        chat = await client.get_entity(group_link)

        if isinstance(chat, Channel):
            if chat.megagroup or chat.broadcast:
                # Handle megagroups and broadcast channels
                user = await client.get_input_entity(contact_id)
                try:
                    await client(InviteToChannelRequest(chat, [user]))
                except Exception as e:
                    return True, f"Failed to invite to channel: {e}"
            else:
                return True, "Unsupported channel type"
        elif isinstance(chat, Chat):
            # Handle basic chats
            user = await client.get_input_entity(contact_id)
            try:
                await client(AddChatUserRequest(chat.id, user, fwd_limit=10))
            except Exception as e:
                return True, f"Failed to invite to chat: {e}"
        else:
            return True, "Unknown chat type"

        return True, "Contact invited successfully"
    except Exception as e:
        return False, f"Error: {e}"
    finally:
        await client.disconnect()

def invite_to_group(session_name, group_link, contact_id):
    return asyncio.run(invite_to_group_async(session_name, group_link, contact_id))

async def invite_members(session_name, group_link, member_ids, member_usernames, min_delay, max_delay):
    session_path = get_session_path(session_name)
    client = TelegramClient(session_path, api_id, api_hash)
    await client.start()

    try:
        chat = await client.get_entity(group_link)
        if isinstance(chat, Channel):
            if chat.megagroup or chat.broadcast:
                for member_id, member_username in zip(member_ids, member_usernames):
                    member_id = int(member_id)
                    user = None
                    try:
                        member_id = int(member_id)
                        user = await client.get_input_entity(member_id)
                        await client(InviteToChannelRequest(chat, [user]))
                    except Exception as e:
                        print(f"Error inviting member ID {member_id}: {e}")
                        try:
                            user = await client.get_input_entity(member_username)
                            await client(InviteToChannelRequest(chat, [user]))
                        except Exception as e:
                            print(f"Error inviting username {member_username}: {e}")
                            return False, f"Failed to invite {member_id} or {member_username}: {str(e)}"

                    delay = random.randint(min_delay, max_delay)
                    print(f"Waiting for {delay} seconds before next invite.")
                    time.sleep(delay)
        elif isinstance(chat, Chat):
            for member_id, member_username in zip(member_ids, member_usernames):
                member_id = int(member_id)
                user = None
                try:
                    member_id = int(member_id)
                    user = await client.get_input_entity(member_id)
                    await client(AddChatUserRequest(chat.id, user, fwd_limit=10))
                except Exception as e:
                    print(f"Error adding member ID {member_id}: {e}")
                    try:
                        user = await client.get_input_entity(member_username)
                        await client(AddChatUserRequest(chat.id, user, fwd_limit=10))
                    except Exception as e:
                        print(f"Error adding username {member_username}: {e}")
                        return False, f"Failed to add {member_id} or {member_username}: {str(e)}"

                delay = random.randint(min_delay, max_delay)
                print(f"Waiting for {delay} seconds before next add.")
                time.sleep(delay)
        else:
            return False, "Unknown chat type"

        return True, "All members invited successfully"
    except Exception as e:
        return False, f"Error: {str(e)}"
    finally:
        await client.disconnect()


def invite_members_sync(session_name, group_link, member_ids, member_usernames, min_delay, max_delay):
    return asyncio.run(invite_members(session_name, group_link, member_ids, member_usernames, min_delay, max_delay))

def process_spintax(text):
    pattern = r"\{([^{}]*)\}"
    while re.search(pattern, text):
        text = re.sub(pattern, lambda match: random.choice(match.group(1).split('|')), text)
    return text

async def send_message_async(session_name, message_text, recipient_ids, recipient_usernames, min_delay=3, max_delay=5, attachment_file_path=None):
    session_path = get_session_path(session_name)
    client = TelegramClient(session_path, api_id, api_hash)
    await client.start()

    try:
        for recipient_id, recipient_username in zip(recipient_ids, recipient_usernames):
            recipient_id = int(recipient_id)
            user = None

            # Process Spintax here for each message
            processed_message = process_spintax(message_text)
            print(f"Original message: {message_text}")
            print(f"Processed message: {processed_message}")

            try:
                # Try sending the message using recipient ID
                user = await client.get_input_entity(recipient_id)
                if attachment_file_path:
                    await client.send_file(user, attachment_file_path, caption=processed_message)
                    print(f"File sent to {recipient_id} with message: {processed_message}")
                else:
                    await client(SendMessageRequest(user, processed_message))
                    print(f"Message sent to {recipient_id}")
            except Exception as e:
                print(f"Error sending message to ID {recipient_id}: {e}")

                # If sending by ID fails, try sending by username
                try:
                    user = await client.get_input_entity(recipient_username)
                    if attachment_file_path:
                        await client.send_file(user, attachment_file_path, caption=processed_message)
                        print(f"File sent to {recipient_username} with message: {processed_message}")
                    else:
                        await client(SendMessageRequest(user, processed_message))
                        print(f"Message sent to {recipient_username}")
                except Exception as e:
                    print(f"Error sending message to username {recipient_username}: {e}")
                    return False, f"Error sending message: {str(e)}"

            # Random delay between messages
            delay = random.randint(min_delay, max_delay)
            print(f"Waiting for {delay} seconds before sending the next message.")
            time.sleep(delay)

        return True, "Messages sent ya"
    except Exception as e:
        return False, f"Error sending message: {str(e)}"
    finally:
        await client.disconnect()

def send_message(session_name, message_text, recipient_ids, recipient_usernames, min_delay=3, max_delay=5, attachment_file_path=None):
    return asyncio.run(send_message_async(session_name, message_text, recipient_ids, recipient_usernames, min_delay, max_delay, attachment_file_path))

def logout_and_delete_session_sync(session_name):
    session_path = get_session_path(session_name)
    client = TelegramClient(session_path, api_id, api_hash)
    try:
        client.connect()
        if client.is_user_authorized():
            client.log_out()
            print(f"Logged out from {session_name}")

        # Dapatkan jalur file sesi
        session_file = f"{session_path}.session"
        if os.path.exists(session_file):
            os.remove(session_file)
            print(f"Deleted session file for {session_name}")

    except Exception as e:
        print(f"Error during logout and session deletion for {session_name}: {e}")
    finally:
        client.disconnect()

async def send_to_user_async(session_name, target, message_text, attachment_file_path=None):
    session_path = get_session_path(session_name)
    client = TelegramClient(session_path, api_id, api_hash)
    await client.start()

    try:
        print(f"Session Name: {session_name}")
        print(f"Target: {target}")
        print(f"Message Text: {message_text}")
        print(f"Attachment File Path: {attachment_file_path}")

        # Try to get the entity as a phone number first
        try:
            user = await client.get_entity(target)
        except Exception as e:
            print(f"Failed to get entity as phone number: {e}. Trying as username.")
            # If it fails, try to get the entity as a username
            user = await client.get_input_entity(target)

        # Directly use the message_text without Spintax processing
        print(f"Sending message: {message_text}")

        try:
            if attachment_file_path:
                await client.send_file(user, attachment_file_path, caption=message_text)
                print(f"File sent to {target} with message: {message_text}")
            else:
                await client(SendMessageRequest(user, message_text))
                print(f"Message sent to {target}")
        except Exception as e:
            print(f"Error sending message to {target}: {e}")
            return False, f"Error sending message: {str(e)}"

        return True, "Message sent ya"
    except Exception as e:
        print(f"Error: {e}")
        return False, f"Error: {str(e)}"
    finally:
        await client.disconnect()

def send_to_user(session_name, target, message_text, attachment_file_path=None):
    return asyncio.run(send_to_user_async(session_name, target, message_text, attachment_file_path))

def extract_chats(session_name, extractor_user_id):
    session_path = get_session_path(session_name)
    client = TelegramClient(session_path, api_id, api_hash)
    client.start()

    try:
        # Ambil entitas dialog
        dialogs = client.get_dialogs()

        # Koneksi ke database
        conn = sqlite3.connect(get_db_path(), timeout=10)
        c = conn.cursor()

        # Hapus chat yang ada untuk extractor user
        c.execute("DELETE FROM user_chats WHERE user_id = ?", (extractor_user_id,))

        for dialog in dialogs:
            if dialog.is_user:
                user = dialog.entity
                chat_id = user.id
                access_hash = user.access_hash
                first_name = user.first_name or ''
                last_name = user.last_name or ''
                username = user.username or ''
                
                # Pastikan semua variabel adalah tipe data yang didukung
                c.execute("INSERT INTO user_chats (user_id, chat_user_id, access_hash, first_name, last_name, username) VALUES (?, ?, ?, ?, ?, ?)",
                          (extractor_user_id, chat_id, access_hash, first_name, last_name, username))
        conn.commit()
    except Exception as e:
        print(f"Error extracting chats: {e}")
    finally:
        conn.close()
        client.disconnect()




