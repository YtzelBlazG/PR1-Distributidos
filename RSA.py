#RSA
import rsa
import psutil
import time
import hashlib

from Crypto.Cipher import AES
from Crypto.Random import get_random_bytes
import os
from ecdsa import SigningKey, VerifyingKey, NIST256p
from Crypto.Cipher import Blowfish

from argon2 import PasswordHasher
from argon2.low_level import hash_secret_raw, Type
from tqdm import tqdm 

iteration_global = 2

#Tiempos de encriptacion
e_time_aes=0
e_time_argon2=0
e_time_birthyear=0
e_time_blowfish=0
e_time_rsa=0
e_time_sha3=0
#Tiempos de desencriptacion
d_time_aes=0
d_time_argon2=0
d_time_birthyear=0
d_time_blowfish=0
d_time_rsa=0
d_time_sha3=0


#RSA
def encrypt_file(input_filename, output_filename, key_size=2048, chunk_size=190, iterations=5):
    global e_time_rsa
    publicKey, privateKey = rsa.newkeys(key_size)

    # Guardar claves en archivos PEM
    with open('public.pem', 'wb') as p:
        p.write(publicKey.save_pkcs1('PEM'))
    with open('private.pem', 'wb') as p:
        p.write(privateKey.save_pkcs1('PEM'))

    with open(input_filename, 'r', encoding='utf-8') as file:
        contents = file.read().encode()

    # Listas para almacenar métricas
    memory_usage_list = []
    cpu_usage_list = []
    time_list = []

    for i in range(iterations):
        start_time = time.time()

        # Barra de progreso para la encriptación
        encrypted_chunks = []
        with tqdm(total=len(contents) // chunk_size, desc=f"Encriptando Iteración {i+1}", unit="bloque") as pbar:
            for j in range(0, len(contents), chunk_size):
                encrypted_chunks.append(rsa.encrypt(contents[j:j + chunk_size], publicKey))
                pbar.update(1)  # Actualizar la barra de progreso
        contents = b''.join(encrypted_chunks)

        # Medir uso de memoria y CPU
        memory_usage = psutil.Process().memory_info().rss / (1024 ** 2)  # En MB
        cpu_usage = psutil.cpu_percent(interval=0.1)
        elapsed_time = time.time() - start_time  # Tiempo transcurrido en segundos

        memory_usage_list.append(memory_usage)
        cpu_usage_list.append(cpu_usage)
        time_list.append(elapsed_time)
    
    # Calcular promedios
    avg_memory = sum(memory_usage_list) / iterations
    avg_cpu = sum(cpu_usage_list) / iterations
    avg_time = sum(time_list) / iterations
    e_time_rsa = avg_time

    print("RSA ", f"Memoria: {avg_memory:.2f} MB, CPU: {avg_cpu:.2f}%, Tiempo: {e_time_rsa:.2f} seg, Iteraciones: {iterations}\n")

    with open(output_filename, 'wb') as file:
        file.write(contents)
def decrypt_file(input_filename, output_filename, chunk_size=256, iterations=5):
    global d_time_rsa
    with open('private.pem', 'rb') as p:
        privateKey = rsa.PrivateKey.load_pkcs1(p.read())

    with open(input_filename, 'rb') as file:
        contents = file.read()

    # Listas para almacenar métricas
    memory_usage_list = []
    cpu_usage_list = []
    time_list = []

    for i in range(iterations):
        start_time = time.time()

        # Barra de progreso para la desencriptación
        decrypted_chunks = []
        with tqdm(total=len(contents) // chunk_size, desc=f"Desencriptando Iteración {i+1}", unit="bloque") as pbar:
            for j in range(0, len(contents), chunk_size):
                decrypted_chunks.append(rsa.decrypt(contents[j:j + chunk_size], privateKey))
                pbar.update(1)  # Actualizar la barra de progreso
        contents = b''.join(decrypted_chunks)

        # Medir uso de memoria y CPU
        memory_usage = psutil.Process().memory_info().rss / (1024 ** 2)  # En MB
        cpu_usage = psutil.cpu_percent(interval=0.1)
        elapsed_time = time.time() - start_time  # Tiempo transcurrido en segundos

        memory_usage_list.append(memory_usage)
        cpu_usage_list.append(cpu_usage)
        time_list.append(elapsed_time)

    # Calcular promedios
    avg_memory = sum(memory_usage_list) / iterations
    avg_cpu = sum(cpu_usage_list) / iterations
    avg_time = sum(time_list) / iterations
    d_time_rsa = avg_time

    print("RSA ", f"Memoria: {avg_memory:.2f} MB, CPU: {avg_cpu:.2f}%, Tiempo: {d_time_rsa:.2f} seg, Iteraciones: {iterations}\n")

    with open(output_filename, 'w', encoding='utf-8') as file:
        file.write(contents.decode())  # Decodificar y guardar el archivo desencriptado
#AES
def encrypt_file_AES(input_filename, output_filename, key_size=256, iterations=1):
    global e_time_aes
    # Verificar si el archivo existe
    if not os.path.exists(input_filename):
        print(f"Error: El archivo '{input_filename}' no existe.")
        return
    
    # Crear directorio si no existe
    os.makedirs(os.path.dirname(output_filename), exist_ok=True)

    key = get_random_bytes(key_size // 8)  # Generar clave de 256 bits
    
    with open('aes_key.bin', 'wb') as key_file:
        key_file.write(key)  # Guardar la clave en un archivo
    
    with open(input_filename, 'rb') as file:
        contents = file.read()

    # Listas para métricas
    memory_usage_list = []
    cpu_usage_list = []
    time_list = []

    # Barra de progreso por iteraciones
    with tqdm(total=iterations, desc="Encriptando", unit="iteración") as pbar:
        for _ in range(iterations):
            start_time = time.time()
            
            iv = get_random_bytes(16)  # Vector de inicialización
            cipher = AES.new(key, AES.MODE_CBC, iv)
            
            padding_length = 16 - len(contents) % 16
            contents_padded = contents + bytes([padding_length]) * padding_length
            encrypted_data = cipher.encrypt(contents_padded)
            contents = iv + encrypted_data  # Añadir IV al principio
            
            memory_usage = psutil.Process().memory_info().rss / (1024 ** 2)
            cpu_usage = psutil.cpu_percent()
            elapsed_time = time.time() - start_time
            
            memory_usage_list.append(memory_usage)
            cpu_usage_list.append(cpu_usage)
            time_list.append(elapsed_time)
            
            pbar.update(1)  # Actualizar la barra de progreso por cada iteración
    
    avg_memory = sum(memory_usage_list) / iterations
    avg_cpu = sum(cpu_usage_list) / iterations
    avg_time = sum(time_list) / iterations
    e_time_aes = avg_time

    print("AES ", f"Memoria: {avg_memory:.2f} MB, CPU: {avg_cpu:.2f}%, Tiempo: {e_time_aes:.2f} seg, Iteraciones: {iterations}\n")
    
    with open(output_filename, 'wb') as file:
        file.write(contents)
def decrypt_file_AES(input_filename, output_filename, iterations=1):
    global d_time_aes
    # Verificar si el archivo cifrado existe
    if not os.path.exists(input_filename):
        print(f"Error: El archivo '{input_filename}' no existe.")
        return

    # Verificar si la clave existe
    if not os.path.exists('aes_key.bin'):
        print("Error: No se encontró la clave de cifrado 'aes_key.bin'.")
        return
    
    with open('aes_key.bin', 'rb') as key_file:
        key = key_file.read()
    
    with open(input_filename, 'rb') as file:
        contents = file.read()
    
    memory_usage_list = []
    cpu_usage_list = []
    time_list = []
    
    # Barra de progreso por iteraciones
    with tqdm(total=iterations, desc="Desencriptando", unit="iteración") as pbar:
        for _ in range(iterations):
            start_time = time.time()
            
            iv = contents[:16]  # Extraer IV
            encrypted_data = contents[16:]
            cipher = AES.new(key, AES.MODE_CBC, iv)
            decrypted_data = cipher.decrypt(encrypted_data)
            
            padding_length = decrypted_data[-1]
            contents = decrypted_data[:-padding_length]
            
            memory_usage = psutil.Process().memory_info().rss / (1024 ** 2)
            cpu_usage = psutil.cpu_percent()
            elapsed_time = time.time() - start_time
            
            memory_usage_list.append(memory_usage)
            cpu_usage_list.append(cpu_usage)
            time_list.append(elapsed_time)
            
            pbar.update(1)  # Actualizar la barra de progreso por cada iteración
    
    avg_memory = sum(memory_usage_list) / iterations
    avg_cpu = sum(cpu_usage_list) / iterations
    avg_time = sum(time_list) / iterations
    d_time_aes = avg_time
    
    os.makedirs(os.path.dirname(output_filename), exist_ok=True)  # Crear carpeta si no existe
    
    with open(output_filename, 'wb') as file:
        file.write(contents)  # Guardar archivo desencriptado

    print("AES ", f"Memoria: {avg_memory:.2f} MB, CPU: {avg_cpu:.2f}%, Tiempo: {d_time_aes:.2f} seg, Iteraciones: {iterations}\n")
#Blowfish
def encrypt_file_Blowfish(input_filename, output_filename, key_size=128, iterations=5):
    """Encripta un archivo usando Blowfish en modo CBC con múltiples iteraciones."""
    global e_time_blowfish
    key = get_random_bytes(key_size // 8)  # Generar clave de 128 bits (16 bytes)
    
    with open('blowfish_key.bin', 'wb') as key_file:
        key_file.write(key)  # Guardar la clave en un archivo
    
    with open(input_filename, 'rb') as file:
        contents = file.read()
    
    memory_usage_list = []
    cpu_usage_list = []
    time_list = []
    
    for _ in range(iterations):
        start_time = time.time()
        
        iv = get_random_bytes(8)  # Vector de inicialización de 8 bytes
        cipher = Blowfish.new(key, Blowfish.MODE_CBC, iv)
        
        # Añadir padding para que los datos sean múltiplos de 8
        padding_length = 8 - len(contents) % 8
        contents_padded = contents + bytes([padding_length]) * padding_length
        
        # Encriptar
        encrypted_data = cipher.encrypt(contents_padded)
        contents = iv + encrypted_data  # Añadir IV al principio
        
        memory_usage = psutil.Process().memory_info().rss / (1024 ** 2)
        cpu_usage = psutil.cpu_percent(interval=0.1)
        elapsed_time = time.time() - start_time
        
        memory_usage_list.append(memory_usage)
        cpu_usage_list.append(cpu_usage)
        time_list.append(elapsed_time)
        
        # Barra de progreso para la encriptación
        with tqdm(total=len(contents) // 8, desc=f"Encriptando Iteración {_+1}", unit="bloque") as pbar:
            for j in range(0, len(contents), 8):
                pbar.update(1)  # Actualizar la barra de progreso
        
    avg_memory = sum(memory_usage_list) / iterations
    avg_cpu = sum(cpu_usage_list) / iterations
    avg_time = sum(time_list) / iterations
    e_time_blowfish = avg_time
    
    print("Blowfish ", f"Memoria: {avg_memory:.2f} MB, CPU: {avg_cpu:.2f}%, Tiempo: {e_time_blowfish:.2f} seg, Iteraciones: {iterations}\n")
    
    with open(output_filename, 'wb') as file:
        file.write(contents)
def decrypt_file_Blowfish(input_filename, output_filename, iterations=5):
    """Desencripta un archivo cifrado con Blowfish en modo CBC."""
    global d_time_blowfish
    with open('blowfish_key.bin', 'rb') as key_file:
        key = key_file.read()
    
    with open(input_filename, 'rb') as file:
        contents = file.read()
    
    memory_usage_list = []
    cpu_usage_list = []
    time_list = []
    
    for _ in range(iterations):
        start_time = time.time()
        
        iv = contents[:8]  # Extraer IV de 8 bytes
        encrypted_data = contents[8:]
        cipher = Blowfish.new(key, Blowfish.MODE_CBC, iv)
        decrypted_data = cipher.decrypt(encrypted_data)
        
        # Quitar padding
        padding_length = decrypted_data[-1]
        contents = decrypted_data[:-padding_length]
        
        memory_usage = psutil.Process().memory_info().rss / (1024 ** 2)
        cpu_usage = psutil.cpu_percent(interval=0.1)
        elapsed_time = time.time() - start_time
        
        memory_usage_list.append(memory_usage)
        cpu_usage_list.append(cpu_usage)
        time_list.append(elapsed_time)
        
        # Barra de progreso para la desencriptación
        with tqdm(total=len(contents) // 8, desc=f"Desencriptando Iteración {_+1}", unit="bloque") as pbar:
            for j in range(0, len(contents), 8):
                pbar.update(1)  # Actualizar la barra de progreso
        
    avg_memory = sum(memory_usage_list) / iterations
    avg_cpu = sum(cpu_usage_list) / iterations
    avg_time = sum(time_list) / iterations
    d_time_blowfish = avg_time
    
    print("Blowfish ", f"Memoria: {avg_memory:.2f} MB, CPU: {avg_cpu:.2f}%, Tiempo: {d_time_blowfish:.2f} seg, Iteraciones: {iterations}\n")
    
    with open(output_filename, 'wb') as file:
        file.write(contents)
#SHA3
def hash_file_SHA3(input_filename, output_filename, iterations=5, block_size=8192):
    """Genera un hash SHA-3-512 de un archivo con múltiples iteraciones, procesando en bloques más grandes."""
    global e_time_sha3
    global d_time_sha3
    
    with open(input_filename, 'rb') as file:
        contents = file.read()
    
    memory_usage_list = []
    cpu_usage_list = []
    time_list = []
    
    for _ in range(iterations):
        start_time = time.time()
        
        # Crear el objeto SHA-3-512
        sha3_hasher = hashlib.sha3_512()
        
        # Barra de progreso para el procesamiento del archivo en bloques
        with tqdm(total=len(contents), desc=f"Generando hash Iteración {_+1}", unit="byte") as pbar:
            for i in range(0, len(contents), block_size):
                block = contents[i:i + block_size]
                sha3_hasher.update(block)  # Actualizar el hash con cada bloque
                pbar.update(len(block))  # Actualizar la barra de progreso por cada bloque procesado
        
        hash_digest = sha3_hasher.digest()
        
        memory_usage = psutil.Process().memory_info().rss / (1024 ** 2)  # Memoria en MB
        cpu_usage = psutil.cpu_percent(interval=0.1)  # Uso de CPU
        elapsed_time = time.time() - start_time  # Tiempo transcurrido
        
        memory_usage_list.append(memory_usage)
        cpu_usage_list.append(cpu_usage)
        time_list.append(elapsed_time)
    
    # Promedios de las métricas
    avg_memory = sum(memory_usage_list) / iterations
    avg_cpu = sum(cpu_usage_list) / iterations
    avg_time = sum(time_list) / iterations
    e_time_sha3 = avg_time
    d_time_sha3 = avg_time  # Se usa el mismo tiempo para desencriptado ya que es el mismo proceso
    
    print("SHA-3-512 ", f"Memoria: {avg_memory:.2f} MB, CPU: {avg_cpu:.2f}%, Tiempo: {e_time_sha3:.2f} seg, Iteraciones: {iterations}\n")
    
    with open(output_filename, 'wb') as file:
        file.write(hash_digest)
def verify_file_SHA3(input_filename, hash_filename):
    """Verifica la integridad de un archivo comparando su hash SHA-3-512."""
    with open(input_filename, 'rb') as file:
        contents = file.read()
    
    with open(hash_filename, 'rb') as file:
        stored_hash = file.read()
    
    sha3_hasher = hashlib.sha3_512()
    sha3_hasher.update(contents)
    computed_hash = sha3_hasher.digest()
#Argon2    
def derive_key(password: str, salt: bytes, key_length=32):
    """Deriva una clave segura a partir de una contraseña usando Argon2."""
    return hash_secret_raw(
        password.encode(), salt,
        time_cost=2, memory_cost=65536, parallelism=1,
        hash_len=key_length, type=Type.ID
    )

def encrypt_file_Argon2(input_filename, output_filename, password, iterations=5):
    """Encripta un archivo usando Argon2 para derivar la clave y AES-GCM para el cifrado."""
    global e_time_argon2
    with open(input_filename, 'rb') as file:
        contents = file.read()
    
    memory_usage_list = []
    cpu_usage_list = []
    time_list = []
    
    salt = get_random_bytes(16)
    key = derive_key(password, salt)
    
    for _ in tqdm(range(iterations), desc="Encrypting", unit="iter"):
        start_time = time.time()
        
        cipher = AES.new(key, AES.MODE_GCM)
        ciphertext, tag = cipher.encrypt_and_digest(contents)
        encrypted_data = salt + cipher.nonce + tag + ciphertext
        
        memory_usage = psutil.Process().memory_info().rss / (1024 ** 2)
        cpu_usage = psutil.cpu_percent(interval=0.1)
        elapsed_time = time.time() - start_time
        
        memory_usage_list.append(memory_usage)
        cpu_usage_list.append(cpu_usage)
        time_list.append(elapsed_time)
    
    avg_memory = sum(memory_usage_list) / iterations
    avg_cpu = sum(cpu_usage_list) / iterations
    avg_time = sum(time_list) / iterations
    e_time_argon2 = avg_time
    
    print(f"Argon2 Memoria: {avg_memory:.2f} MB, CPU: {avg_cpu:.2f}%, Tiempo: {e_time_argon2:.2f} seg, Iteraciones: {iterations}\n")
    
    with open(output_filename, 'wb') as file:
        file.write(encrypted_data)

def decrypt_file_Argon2(input_filename, output_filename, password, iterations=5):
    """Desencripta un archivo cifrado con Argon2 y AES-GCM."""
    global d_time_argon2
    with open(input_filename, 'rb') as file:
        contents = file.read()
    
    memory_usage_list = []
    cpu_usage_list = []
    time_list = []
    
    salt = contents[:16]
    nonce = contents[16:32]
    tag = contents[32:48]
    ciphertext = contents[48:]
    
    key = derive_key(password, salt)
    
    for _ in tqdm(range(iterations), desc="Decrypting", unit="iter"):
        start_time = time.time()
        
        cipher = AES.new(key, AES.MODE_GCM, nonce=nonce)
        decrypted_data = cipher.decrypt_and_verify(ciphertext, tag)
        
        memory_usage = psutil.Process().memory_info().rss / (1024 ** 2)
        cpu_usage = psutil.cpu_percent(interval=0.1)
        elapsed_time = time.time() - start_time
        
        memory_usage_list.append(memory_usage)
        cpu_usage_list.append(cpu_usage)
        time_list.append(elapsed_time)
    
    avg_memory = sum(memory_usage_list) / iterations
    avg_cpu = sum(cpu_usage_list) / iterations
    avg_time = sum(time_list) / iterations
    d_time_argon2 = avg_time
    
    print(f"Argon2 Memoria: {avg_memory:.2f} MB, CPU: {avg_cpu:.2f}%, Tiempo: {d_time_argon2:.2f} seg, Iteraciones: {iterations}\n")
    
    with open(output_filename, 'wb') as file:
        file.write(decrypted_data)

#BirthYear
def encrypt_BirthYear(word, birth_year, iterations=iteration_global):
    """Convierte cada carácter a su valor ASCII (o UTF-8 si es necesario),
    le resta el año de nacimiento y guarda en un archivo."""
    global e_time_birthyear
    encrypted_values = []
    
    memory_usage_list = []
    cpu_usage_list = []
    time_list = []

    # Crear la carpeta si no existe
    os.makedirs("BirthYear", exist_ok=True)

    for _ in range(iterations):
        start_time = time.time()

        # Crear la barra de progreso
        encrypted_values = []
        with tqdm(total=len(word), desc=f"Encriptando Iteración {_+1}", unit="carácter") as pbar:
            for char in word:
                # Encriptar cada carácter
                encrypted_value = (ord(char) - birth_year) if ord(char) < 128 else int.from_bytes(char.encode("utf-8"), "big") - birth_year
                encrypted_values.append(encrypted_value)
                pbar.update(1)  # Actualizar barra de progreso por cada carácter

        memory_usage = psutil.Process().memory_info().rss / (1024 ** 2)  # MB
        cpu_usage = psutil.cpu_percent(interval=0.1)
        elapsed_time = time.time() - start_time

        memory_usage_list.append(memory_usage)
        cpu_usage_list.append(cpu_usage)
        time_list.append(elapsed_time)

    avg_memory = sum(memory_usage_list) / iterations
    avg_cpu = sum(cpu_usage_list) / iterations
    avg_time = sum(time_list) / iterations
    e_time_birthyear = avg_time
    print("BirthYear ", f"Memoria: {avg_memory:.2f} MB, CPU: {avg_cpu:.2f}%, Tiempo: {e_time_birthyear:.4f} seg, Iteraciones: {iterations}\n")

    # Guardar en archivo
    with open("BirthYear/encrypted_BirthYear.txt", "w", encoding="utf-8") as file:
        file.write(" ".join(map(str, encrypted_values)))
    return encrypted_values
def decrypt_BirthYear(encrypted_values, birth_year, iterations=iteration_global):
    """Suma el año de nacimiento a los valores ASCII/UTF-8,
    reconstruye la palabra y guarda en un archivo."""
    global d_time_birthyear
    decrypted_word = ""

    memory_usage_list = []
    cpu_usage_list = []
    time_list = []

    for _ in range(iterations):
        start_time = time.time()

        # Crear la barra de progreso
        decrypted_word = ""
        with tqdm(total=len(encrypted_values), desc=f"Desencriptando Iteración {_+1}", unit="valor") as pbar:
            for num in encrypted_values:
                # Desencriptar cada valor
                decrypted_char = chr(num + birth_year) if num + birth_year < 128 else bytes.fromhex(hex(num + birth_year)[2:]).decode("utf-8")
                decrypted_word += decrypted_char
                pbar.update(1)  # Actualizar barra de progreso por cada valor

        memory_usage = psutil.Process().memory_info().rss / (1024 ** 2)  # MB
        cpu_usage = psutil.cpu_percent(interval=0.1)
        elapsed_time = time.time() - start_time

        memory_usage_list.append(memory_usage)
        cpu_usage_list.append(cpu_usage)
        time_list.append(elapsed_time)

    avg_memory = sum(memory_usage_list) / iterations
    avg_cpu = sum(cpu_usage_list) / iterations
    avg_time = sum(time_list) / iterations
    d_time_birthyear = avg_time

    print("BirthYear ", f"Memoria: {avg_memory:.2f} MB, CPU: {avg_cpu:.2f}%, Tiempo: {d_time_birthyear:.4f} seg, Iteraciones: {iterations}\n")

    # Guardar en archivo
    with open("BirthYear/decrypted_BirthYear.txt", "w", encoding="utf-8") as file:
        file.write(decrypted_word)
    return decrypted_word

# Ejecutar el proceso con medición

print("PROMEDIO DE ENCRIPTACION")
#RSA
encrypt_file('archivo.txt', 'RSA/encrypted_file_RSA.txt', iterations=iteration_global)
#AES
encrypt_file_AES('archivo.txt', 'AES/encrypted_file_AES.txt', iterations=iteration_global)
#Blowfish
encrypt_file_Blowfish('archivo.txt', 'Blowfish/encrypted_file_Blowfish.txt', iterations=iteration_global)
#SHA3
hash_file_SHA3('archivo.txt', 'SHA3/encrypted_file_SHA3.txt', iterations=iteration_global)
verify_file_SHA3('SHA3/encrypted_file_SHA3.txt', 'SHA3/decrypted_file_SHA3.txt')
#Argon2
password = "MiContraseñaSegura"
encrypt_file_Argon2('archivo.txt', 'Argon2/encrypted_file_Argon2.txt',password, iterations=iteration_global)
#BirthYear
word = input("Ingresa una palabra para encriptar: ")
birth_year = int(input("Ingresa tu año de nacimiento: "))
encrypted = encrypt_BirthYear(word, birth_year)
print("---------------------------------------------------------------------------------------")
print("PROMEDIO DE DESENCRIPTACION")
#RSA
decrypt_file('RSA/encrypted_file_RSA.txt', 'RSA/decrypted_file_RSA.txt', iterations=iteration_global)
#AES
decrypt_file_AES('AES/encrypted_file_AES.txt', 'AES/decrypted_file_AES.txt', iterations=iteration_global)
#Blowfish
decrypt_file_Blowfish('Blowfish/encrypted_file_Blowfish.txt', 'Blowfish/decrypted_file_Blowfish.txt', iterations=iteration_global)
#SHA3
hash_file_SHA3('archivo.txt', 'SHA3/encrypted_file_SHA3.txt', iterations=iteration_global)
verify_file_SHA3('SHA3/encrypted_file_SHA3.txt', 'SHA3/decrypted_file_SHA3.txt')
#Argon2
password = "MiContraseñaSegura"
decrypt_file_Argon2('Argon2/encrypted_file_Argon2.txt', 'Argon2/decrypted_file_Argon2.txt',password, iterations=iteration_global)
#BirthYear
decrypted = decrypt_BirthYear(encrypted, birth_year)
print("--------------------BirthYear--------------------------")
print(f"Palabra encriptada: {encrypted}")
print(f"Palabra desencriptada: {decrypted}")
print("-------------------------------------------------------")
def listar_algoritmos_por_tiempo(e_time_aes, e_time_argon2, e_time_birthyear, e_time_blowfish, e_time_rsa, e_time_sha3,
                                  d_time_aes, d_time_argon2, d_time_birthyear, d_time_blowfish, d_time_rsa, d_time_sha3):
    # Diccionario con los tiempos de encriptación
    encriptacion_tiempos = {
        'AES': e_time_aes,
        'Argon2': e_time_argon2,
        'BirthYear': e_time_birthyear,
        'Blowfish': e_time_blowfish,
        'RSA': e_time_rsa,
        'SHA-3-512': e_time_sha3
    }
    
    # Diccionario con los tiempos de desencriptación
    desencriptacion_tiempos = {
        'AES': d_time_aes,
        'Argon2': d_time_argon2,
        'BirthYear': d_time_birthyear,
        'Blowfish': d_time_blowfish,
        'RSA': d_time_rsa,
        'SHA-3-512': d_time_sha3
    }
    
    # Ordenar los algoritmos de encriptación de más rápido a más lento
    encriptacion_ordenada = sorted(encriptacion_tiempos.items(), key=lambda x: x[1])
    
    # Ordenar los algoritmos de desencriptación de más rápido a más lento
    desencriptacion_ordenada = sorted(desencriptacion_tiempos.items(), key=lambda x: x[1])
    
    return encriptacion_ordenada, desencriptacion_ordenada

# Ejemplo de uso
encriptacion_ordenada, desencriptacion_ordenada = listar_algoritmos_por_tiempo(
    e_time_aes, e_time_argon2, e_time_birthyear, e_time_blowfish, e_time_rsa, e_time_sha3,
    d_time_aes, d_time_argon2, d_time_birthyear, d_time_blowfish, d_time_rsa, d_time_sha3
)
print("--------------------MEJOR TIEMPOR DE ENCRIPTECION--------------------------")
print("Algoritmos de encriptación de más rápido a más lento:")
for algoritmo, tiempo in encriptacion_ordenada:
    print(f"{algoritmo}: {tiempo:.2f} segundos")  # Mostrar 4 decimales
print("--------------------MEJOR TIEMPOR DE DESENCRIPTECION--------------------------")
print("\nAlgoritmos de desencriptación de más rápido a más lento:")
for algoritmo, tiempo in desencriptacion_ordenada:
    print(f"{algoritmo}: {tiempo:.2f} segundos")  # Mostrar 4 decimales







