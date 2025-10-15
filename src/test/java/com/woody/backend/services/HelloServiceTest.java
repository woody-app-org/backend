package com.woody.backend.services;

import static org.junit.jupiter.api.Assertions.assertEquals;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.TestInstance;
import org.junit.jupiter.api.TestInstance.Lifecycle;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.test.context.ActiveProfiles;

import com.woody.backend.domain.dto.HelloDto;
import com.woody.backend.domain.service.HelloImplService;
import com.woody.backend.infraestructure.entity.Hello;

import jakarta.transaction.Transactional;

@SpringBootTest
@ActiveProfiles("test")
@Transactional
@TestInstance(Lifecycle.PER_CLASS)
public class HelloServiceTest {
    
    @Autowired
    private HelloImplService helloService;

    @Test
    public void GetHelloShouldReturnTheSame(){
        var hello = this.helloService.getHello().get(0);
        var shouldBeEqual = new HelloDto("Hello Test!");

        assertEquals(hello, shouldBeEqual);
    }
}
